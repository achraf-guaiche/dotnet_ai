using FluentAssertions;
using Formations.Api.Controllers;
using Formations.Application.Common.Interfaces;
using Formations.Application.Modules.DTOs;
using Formations.Domain.Entities;
using Formations.Domain.Enums;
using Formations.Infrastructure.Persistence;
using Formations.Tests.Unit.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Formations.Tests.Unit.Modules;

// Exercices : persistance des tentatives + progression (le module se complète quand le quiz est
// réussi ET tous les exercices corrects).
public class ExerciceProgressionTests
{
    private const string UserId = "user-1";

    private static ModulesController Build(ApplicationDbContext ctx)
    {
        var c = new ModulesController(ctx, Mock.Of<ICourseGenerationService>());
        DbContextFactory.SetUser(c, UserId);
        return c;
    }

    // Formation : 1 module avec 1 exercice auto-corrigé (réponse « 42 ») + 1 quiz (1 question « A »).
    private static ApplicationDbContext Seed()
    {
        var ctx = DbContextFactory.CreateInMemory();
        var module = new Module
        {
            Id = 1, FormationId = 1, Titre = "M1",
            Formation = new Formation { Id = 1, Titre = "F", CategorieId = 1 },
            Exercices = new List<Exercice>
            {
                new() { Id = 10, ModuleId = 1, Enonce = "2+40 ?", TypeReponse = TypeReponse.TexteLibre, CorrectionAuto = true, ReponseCorrecte = "42", Points = 10 },
            },
            Examens = new List<Examen>
            {
                new() { Id = 20, ModuleId = 1, Titre = "Quiz", NoteMinPassage = 50, TentativesMax = 3,
                    Questions = new List<QuestionQuiz> { new() { Id = 30, ExamenId = 20, Enonce = "?", BonneReponse = "A", Points = 1 } } },
            },
        };
        ctx.Modules.Add(module);
        ctx.Inscriptions.Add(new Inscription { Id = 1, UserId = UserId, FormationId = 1, TotalModules = 1 });
        ctx.SaveChanges();
        return ctx;
    }

    [Fact]
    public async Task SubmitExercice_WrongAnswer_SavedAsIncorrect_NoProgress()
    {
        var ctx = Seed();
        var controller = Build(ctx);

        var result = await controller.SubmitExercice(10, new SubmitExerciceRequest("999"), default);

        var payload = (result as OkObjectResult)!.Value.Should().BeOfType<SubmitExerciceResult>().Subject;
        payload.IsCorrect.Should().BeFalse();
        ctx.ResultatsExercices.Should().ContainSingle(r => r.ExerciceId == 10 && !r.IsCorrect);
        ctx.Inscriptions.Single().ProgressionPourcent.Should().Be(0m);
    }

    [Fact]
    public async Task ModuleComplete_RequiresBothExerciceAndQuiz()
    {
        var ctx = Seed();
        var controller = Build(ctx);

        // Bon exercice seul : module pas encore complet (quiz non réussi).
        await controller.SubmitExercice(10, new SubmitExerciceRequest("42"), default);
        ctx.Inscriptions.Single().ProgressionPourcent.Should().Be(0m);

        // Quiz réussi → module complet → 100 %.
        await controller.AttemptExamen(20, new ExamenAttemptRequest(20,
            new Dictionary<int, string> { [30] = "A" }, 5), default);

        var inscription = ctx.Inscriptions.Single();
        inscription.ProgressionPourcent.Should().Be(100m);
        inscription.Statut.Should().Be(StatutInscription.Termine);
        inscription.NumeroCertificat.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SubmitExercice_Retry_KeepsAttemptHistoryAndReflectsLatest()
    {
        var ctx = Seed();
        var controller = Build(ctx);

        await controller.SubmitExercice(10, new SubmitExerciceRequest("999"), default); // tentative 1 (faux)
        await controller.SubmitExercice(10, new SubmitExerciceRequest("42"), default);  // tentative 2 (bon)

        ctx.ResultatsExercices.Count(r => r.ExerciceId == 10).Should().Be(2);

        var mes = (await controller.MesResultats(1, default) as OkObjectResult)!.Value
            .Should().BeOfType<MesResultatsModuleDto>().Subject;
        mes.Exercices.Should().ContainSingle(); // dernière tentative uniquement
        mes.Exercices[0].TentativeNumero.Should().Be(2);
        mes.Exercices[0].IsCorrect.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitExercice_NoAutoCorrection_AcceptedAsCorrect()
    {
        var ctx = DbContextFactory.CreateInMemory();
        ctx.Exercices.Add(new Exercice { Id = 10, ModuleId = 1, Enonce = "Rédigez…", TypeReponse = TypeReponse.TexteLibre, CorrectionAuto = false, Points = 10, Module = new Module { Id = 1, FormationId = 1, Titre = "M", Formation = new Formation { Id = 1, Titre = "F", CategorieId = 1 } } });
        ctx.SaveChanges();
        var controller = Build(ctx);

        var payload = ((await controller.SubmitExercice(10, new SubmitExerciceRequest("ma réponse libre"), default)) as OkObjectResult)!
            .Value.Should().BeOfType<SubmitExerciceResult>().Subject;

        payload.CorrectionAuto.Should().BeFalse();
        payload.IsCorrect.Should().BeTrue(); // soumission acceptée
    }
}
