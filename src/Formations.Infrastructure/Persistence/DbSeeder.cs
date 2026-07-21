using Formations.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Formations.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger logger)
    {
        await context.Database.MigrateAsync();

        await SeedRolesAsync(roleManager, logger);
        await SeedAdminUserAsync(userManager, logger);
        await SeedCategoriesAsync(context, logger);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        string[] roles = ["Admin", "Formateur", "Apprenant"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Rôle '{Role}' créé", role);
            }
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        const string adminEmail = "admin@formations.local";
        if (await userManager.FindByEmailAsync(adminEmail) is not null) return;

        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "Administrateur",
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await userManager.CreateAsync(admin, "Admin@1234!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
            logger.LogInformation("Admin créé : {Email} / Admin@1234!", adminEmail);
        }
    }

    private static async Task SeedCategoriesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.Categories.AnyAsync()) return;

        context.Categories.AddRange(
            new Categorie { Nom = "Développement Web", Description = "HTML, CSS, JavaScript, frameworks modernes", Icone = "💻" },
            new Categorie { Nom = "Data Science", Description = "Python, Machine Learning, statistiques", Icone = "📊" },
            new Categorie { Nom = "DevOps", Description = "Docker, CI/CD, Cloud, Kubernetes", Icone = "⚙️" },
            new Categorie { Nom = "Design UX/UI", Description = "Figma, design thinking, prototypage", Icone = "🎨" },
            new Categorie { Nom = "Sécurité Informatique", Description = "Cybersécurité, ethical hacking, OWASP", Icone = "🔒" },
            new Categorie { Nom = "Management", Description = "Leadership, gestion de projet, Scrum", Icone = "📋" }
        );

        await context.SaveChangesAsync();
        logger.LogInformation("Catégories initiales créées");
    }
}
