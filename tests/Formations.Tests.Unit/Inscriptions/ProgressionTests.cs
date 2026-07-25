using FluentAssertions;
using Formations.Api.Controllers;
using Formations.Application.Common.Interfaces;
using Formations.Application.Inscriptions.DTOs;
using Formations.Domain.Entities;
using Formations.Domain.Enums;
using Formations.Infrastructure.Persistence;
using Formations.Tests.Unit.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Formations.Tests.Unit.Inscriptions;

// DETTE-01 : couverture du calcul de progression (InscriptionsController.UpdateProgress).
public class ProgressionTests
{
    private const string UserId = "user-1";

    private static (InscriptionsController controller, ApplicationDbContext ctx, Inscription inscription) Setup(
        int totalModules = 4, int modulesCompletes = 0, StatutInscription statut = StatutInscription.EnCours)
    {
        var ctx = DbContextFactory.CreateInMemory();
        var inscription = new Inscription
        {
            Id = 1,
            UserId = UserId,
            FormationId = 1,
            Formation = new Formation { Id = 1, Titre = "F", CategorieId = 1 },
            Statut = statut,
            TotalModules = totalModules,
            ModulesCompletes = modulesCompletes,
        };
        ctx.Inscriptions.Add(inscription);
        ctx.SaveChanges();

        var controller = new InscriptionsController(
            ctx, Mock.Of<ICertificateService>(), Mock.Of<IEmailService>());
        DbContextFactory.SetUser(controller, UserId);
        return (controller, ctx, inscription);
    }

    [Fact]
    public async Task UpdateProgress_HalfModules_GivesFiftyPercent()
    {
        var (controller, ctx, _) = Setup(totalModules: 4, modulesCompletes: 1);

        await controller.UpdateProgress(1, new UpdateProgressRequest(2, true), default);

        var updated = ctx.Inscriptions.Single(i => i.Id == 1);
        updated.ModulesCompletes.Should().Be(2);
        updated.ProgressionPourcent.Should().Be(50m);
        updated.Statut.Should().Be(StatutInscription.EnCours);
    }

    [Fact]
    public async Task UpdateProgress_ReachingAllModules_CompletesAndIssuesCertificate()
    {
        var (controller, ctx, _) = Setup(totalModules: 2, modulesCompletes: 1);

        await controller.UpdateProgress(1, new UpdateProgressRequest(2, true), default);

        var updated = ctx.Inscriptions.Single(i => i.Id == 1);
        updated.ProgressionPourcent.Should().Be(100m);
        updated.Statut.Should().Be(StatutInscription.Termine);
        updated.DateTerminee.Should().NotBeNull();
        updated.NumeroCertificat.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateProgress_Uncompleting_DecrementsWithoutGoingNegative()
    {
        var (controller, ctx, _) = Setup(totalModules: 4, modulesCompletes: 0);

        await controller.UpdateProgress(1, new UpdateProgressRequest(1, false), default);

        var updated = ctx.Inscriptions.Single(i => i.Id == 1);
        updated.ModulesCompletes.Should().Be(0);
        updated.ProgressionPourcent.Should().Be(0m);
    }

    [Fact]
    public async Task UpdateProgress_CompletingNeverExceedsTotal()
    {
        var (controller, ctx, _) = Setup(totalModules: 2, modulesCompletes: 2, statut: StatutInscription.Termine);

        await controller.UpdateProgress(1, new UpdateProgressRequest(3, true), default);

        var updated = ctx.Inscriptions.Single(i => i.Id == 1);
        updated.ModulesCompletes.Should().Be(2); // plafonné à TotalModules
        updated.ProgressionPourcent.Should().Be(100m);
    }

    [Fact]
    public async Task UpdateProgress_ForeignInscription_ReturnsNotFound()
    {
        var (controller, _, _) = Setup();
        DbContextFactory.SetUser(controller, "someone-else");

        var result = await controller.UpdateProgress(1, new UpdateProgressRequest(1, true), default);

        result.Should().BeOfType<NotFoundResult>();
    }
}
