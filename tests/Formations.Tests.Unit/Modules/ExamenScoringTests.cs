using FluentAssertions;
using Formations.Api.Controllers;
using Formations.Application.Common.Interfaces;
using Formations.Application.Modules.DTOs;
using Formations.Domain.Entities;
using Formations.Infrastructure.Persistence;
using Formations.Tests.Unit.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Formations.Tests.Unit.Modules;

// DETTE-01 : couverture du scoring d'examen (ModulesController.AttemptExamen).
public class ExamenScoringTests
{
    private const string UserId = "user-1";

    private static (ModulesController controller, ApplicationDbContext ctx) Setup(
        decimal noteMinPassage = 60, int tentativesMax = 3)
    {
        var ctx = DbContextFactory.CreateInMemory();
        var examen = new Examen
        {
            Id = 1,
            ModuleId = 1,
            Module = new Module
            {
                Id = 1,
                FormationId = 1,
                Titre = "Module",
                Formation = new Formation { Id = 1, Titre = "F", CategorieId = 1 },
            },
            Titre = "Quiz",
            NoteMinPassage = noteMinPassage,
            TentativesMax = tentativesMax,
            Questions = new List<QuestionQuiz>
            {
                new() { Id = 1, ExamenId = 1, Enonce = "Q1", BonneReponse = "A", Points = 1 },
                new() { Id = 2, ExamenId = 1, Enonce = "Q2", BonneReponse = "B", Points = 1 },
                new() { Id = 3, ExamenId = 1, Enonce = "Q3", BonneReponse = "C", Points = 2 },
            },
        };
        ctx.Examens.Add(examen);
        ctx.SaveChanges();

        var controller = new ModulesController(ctx, Mock.Of<ICourseGenerationService>());
        DbContextFactory.SetUser(controller, UserId);
        return (controller, ctx);
    }

    [Fact]
    public async Task AttemptExamen_AllCorrect_ScoresHundredAndPasses()
    {
        var (controller, ctx) = Setup();
        var request = new ExamenAttemptRequest(1,
            new Dictionary<int, string> { [1] = "A", [2] = "B", [3] = "C" }, 42);

        var result = await controller.AttemptExamen(1, request, default);

        var payload = (result as OkObjectResult)!.Value.Should().BeOfType<ExamenAttemptResult>().Subject;
        payload.Score.Should().Be(100m);
        payload.IsReussi.Should().BeTrue();
        payload.CorrectCount.Should().Be(3);
        payload.TentativeNumero.Should().Be(1);
        ctx.ResultatsExamens.Should().ContainSingle(r => r.UserId == UserId && r.Score == 100m);
    }

    [Fact]
    public async Task AttemptExamen_PartialByPoints_UsesWeightedScore()
    {
        // Q3 vaut 2 points sur 4 → une seule bonne réponse (Q3) = 50 %.
        var (controller, _) = Setup(noteMinPassage: 60);
        var request = new ExamenAttemptRequest(1,
            new Dictionary<int, string> { [1] = "X", [2] = "Y", [3] = "C" }, 10);

        var result = await controller.AttemptExamen(1, request, default);

        var payload = (result as OkObjectResult)!.Value.Should().BeOfType<ExamenAttemptResult>().Subject;
        payload.Score.Should().Be(50m);
        payload.IsReussi.Should().BeFalse(); // 50 < 60
        payload.CorrectCount.Should().Be(1);
    }

    [Fact]
    public async Task AttemptExamen_AnswerCaseInsensitiveAndTrimmed()
    {
        var (controller, _) = Setup();
        var request = new ExamenAttemptRequest(1,
            new Dictionary<int, string> { [1] = " a ", [2] = "b", [3] = "c" }, 10);

        var result = await controller.AttemptExamen(1, request, default);

        var payload = (result as OkObjectResult)!.Value.Should().BeOfType<ExamenAttemptResult>().Subject;
        payload.Score.Should().Be(100m);
    }

    [Fact]
    public async Task AttemptExamen_WhenMaxAttemptsReached_ReturnsBadRequest()
    {
        var (controller, ctx) = Setup(tentativesMax: 2);
        ctx.ResultatsExamens.Add(new ResultatExamen { UserId = UserId, ExamenId = 1, TentativeNumero = 1 });
        ctx.ResultatsExamens.Add(new ResultatExamen { UserId = UserId, ExamenId = 1, TentativeNumero = 2 });
        ctx.SaveChanges();

        var request = new ExamenAttemptRequest(1,
            new Dictionary<int, string> { [1] = "A", [2] = "B", [3] = "C" }, 10);

        var result = await controller.AttemptExamen(1, request, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AttemptExamen_UnknownExamen_ReturnsNotFound()
    {
        var (controller, _) = Setup();
        var request = new ExamenAttemptRequest(999, new Dictionary<int, string>(), 0);

        var result = await controller.AttemptExamen(999, request, default);

        result.Should().BeOfType<NotFoundResult>();
    }
}
