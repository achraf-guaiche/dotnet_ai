using FluentAssertions;
using Formations.Api.Controllers;
using Formations.Application.Admin.DTOs;
using Formations.Domain.Entities;
using Formations.Infrastructure.Persistence;
using Formations.Tests.Unit.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace Formations.Tests.Unit.Admin;

// DETTE-03 : promouvoir un compte en Formateur doit créer la fiche Formateur associée.
public class ChangeRoleTests
{
    private static Mock<UserManager<ApplicationUser>> MockUserManager(ApplicationUser user, IList<string> currentRoles)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mgr = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mgr.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        mgr.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(currentRoles);
        mgr.Setup(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);
        mgr.Setup(m => m.AddToRoleAsync(user, It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        return mgr;
    }

    [Fact]
    public async Task ChangeRole_ToFormateur_CreatesFormateurProfile()
    {
        var ctx = DbContextFactory.CreateInMemory();
        var user = new ApplicationUser { Id = "u1", Email = "u1@test.com", FullName = "U1" };
        var controller = new AdminController(ctx, MockUserManager(user, new List<string> { "Apprenant" }).Object);

        await controller.ChangeRole("u1", new ChangeUserRoleRequest("Formateur"), default);

        ctx.Formateurs.Should().ContainSingle(f => f.UserId == "u1");
    }

    [Fact]
    public async Task ChangeRole_ToFormateur_WhenProfileExists_DoesNotDuplicate()
    {
        var ctx = DbContextFactory.CreateInMemory();
        var user = new ApplicationUser { Id = "u1", Email = "u1@test.com", FullName = "U1" };
        ctx.Formateurs.Add(new Formateur { UserId = "u1" });
        ctx.SaveChanges();
        var controller = new AdminController(ctx, MockUserManager(user, new List<string> { "Formateur" }).Object);

        await controller.ChangeRole("u1", new ChangeUserRoleRequest("Formateur"), default);

        ctx.Formateurs.Count(f => f.UserId == "u1").Should().Be(1);
    }

    [Fact]
    public async Task ChangeRole_ToApprenant_DoesNotCreateFormateurProfile()
    {
        var ctx = DbContextFactory.CreateInMemory();
        var user = new ApplicationUser { Id = "u1", Email = "u1@test.com", FullName = "U1" };
        var controller = new AdminController(ctx, MockUserManager(user, new List<string> { "Formateur" }).Object);

        await controller.ChangeRole("u1", new ChangeUserRoleRequest("Apprenant"), default);

        ctx.Formateurs.Should().BeEmpty();
    }
}
