using System.Security.Claims;
using Formations.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Formations.Tests.Unit.TestHelpers;

/// <summary>
/// Fabrique un ApplicationDbContext EF Core InMemory isolé (base unique par test)
/// et fournit les utilitaires pour injecter un utilisateur authentifié dans un contrôleur.
/// </summary>
public static class DbContextFactory
{
    public static ApplicationDbContext CreateInMemory()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;
        return new ApplicationDbContext(options);
    }

    /// <summary>Attache un utilisateur (claim NameIdentifier) au ControllerContext.</summary>
    public static void SetUser(ControllerBase controller, string userId)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "Test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        };
    }
}
