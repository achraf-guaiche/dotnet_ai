using FluentAssertions;
using Formations.Api.Controllers;
using Formations.Application.Modules.DTOs;
using Formations.Domain.Entities;
using Formations.Infrastructure.Services;
using Formations.Tests.Unit.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Formations.Tests.Unit.Modules;

// Génération IA du contenu de module : en mode mock (aucune clé LLM), le service produit
// un contenu déterministe qui doit être persisté (cours + examen + questions).
public class CourseGenerationTests
{
    [Fact]
    public async Task GenerateModule_MockMode_PersistsCoursExamenAndQuestions()
    {
        var ctx = DbContextFactory.CreateInMemory();
        ctx.Modules.Add(new Module
        {
            Id = 1,
            FormationId = 1,
            Titre = "Introduction",
            Formation = new Formation { Id = 1, Titre = "Formation Test", CategorieId = 1 },
        });
        ctx.SaveChanges();

        // Mode mock : configuration sans LLM:ApiKey → contenu simulé, aucun appel réseau.
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var generator = new AvatarIAService(new HttpClient(), config, NullLogger<AvatarIAService>.Instance);

        var controller = new ModulesController(ctx, generator);
        DbContextFactory.SetUser(controller, "admin");

        var result = await controller.GenerateModule(1, new GenerateModuleRequest("Un cours d'introduction."), default);

        var payload = (result as OkObjectResult)!.Value.Should().BeOfType<GeneratedModuleResult>().Subject;
        payload.NbQuestionsGenerees.Should().BeGreaterThan(0);
        payload.Contenu.Should().NotBeNullOrWhiteSpace();
        payload.TextePourAvatar.Should().NotBeNullOrWhiteSpace();

        ctx.Cours.Should().ContainSingle(c => c.ModuleId == 1);
        ctx.Examens.Should().ContainSingle(e => e.ModuleId == 1);
        ctx.QuestionsQuiz.Count().Should().Be(payload.NbQuestionsGenerees);
    }

    [Fact]
    public async Task GenerateModule_Regenerate_ReplacesQuestionsWithoutDuplicating()
    {
        var ctx = DbContextFactory.CreateInMemory();
        ctx.Modules.Add(new Module
        {
            Id = 1,
            FormationId = 1,
            Titre = "Introduction",
            Formation = new Formation { Id = 1, Titre = "Formation Test", CategorieId = 1 },
        });
        ctx.SaveChanges();

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var generator = new AvatarIAService(new HttpClient(), config, NullLogger<AvatarIAService>.Instance);
        var controller = new ModulesController(ctx, generator);
        DbContextFactory.SetUser(controller, "admin");

        var first = (await controller.GenerateModule(1, new GenerateModuleRequest("v1"), default) as OkObjectResult)!
            .Value as GeneratedModuleResult;
        await controller.GenerateModule(1, new GenerateModuleRequest("v2"), default);

        // Toujours un seul cours, un seul examen, et le même nombre de questions (remplacées, non cumulées).
        ctx.Cours.Should().ContainSingle(c => c.ModuleId == 1);
        ctx.Examens.Should().ContainSingle(e => e.ModuleId == 1);
        ctx.QuestionsQuiz.Count().Should().Be(first!.NbQuestionsGenerees);
    }

    [Fact]
    public async Task GenerateModule_EmptyPrompt_ReturnsBadRequest()
    {
        var ctx = DbContextFactory.CreateInMemory();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var generator = new AvatarIAService(new HttpClient(), config, NullLogger<AvatarIAService>.Instance);
        var controller = new ModulesController(ctx, generator);
        DbContextFactory.SetUser(controller, "admin");

        var result = await controller.GenerateModule(1, new GenerateModuleRequest("   "), default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
