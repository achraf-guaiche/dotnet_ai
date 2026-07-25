using System.Text.Json;
using Formations.Domain.Entities;
using Formations.Domain.Enums;
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
        await SeedFormationsAsync(context, logger);
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

    // Charge les formations de démonstration depuis le JSON embarqué (formations.seed.json).
    private static async Task SeedFormationsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.Formations.AnyAsync()) return;

        var seed = LoadSeedJson();
        if (seed?.Formations is null || seed.Formations.Count == 0) return;

        var categories = await context.Categories.ToDictionaryAsync(c => c.Nom, c => c.Id);

        foreach (var f in seed.Formations)
        {
            if (f.Categorie is null || !categories.TryGetValue(f.Categorie, out var categorieId))
            {
                logger.LogWarning("Seed : catégorie « {Cat} » introuvable pour la formation « {Titre} » — ignorée", f.Categorie, f.Titre);
                continue;
            }

            var formation = new Formation
            {
                Titre = f.Titre ?? "Sans titre",
                Description = f.Description,
                CategorieId = categorieId,
                Niveau = ParseEnum(f.Niveau, NiveauFormation.Debutant),
                DureeHeures = f.DureeHeures,
                Statut = ParseEnum(f.Statut, StatutFormation.Publie),
                Prix = f.Prix,
                Objectifs = f.Objectifs,
                Prerequis = f.Prerequis,
                Modules = (f.Modules ?? new()).Select(m => new Module
                {
                    Titre = m.Titre ?? "Module",
                    Ordre = m.Ordre,
                    Description = m.Description,
                    IsPublie = true,
                    Cours = m.Cours is null ? null : new Cours
                    {
                        Contenu = m.Cours.Contenu,
                        TextePourAvatar = m.Cours.TextePourAvatar,
                        VideoUrl = m.Cours.VideoUrl,
                        PdfUrl = m.Cours.PdfUrl,
                        DureeEstimeeMinutes = m.Cours.DureeEstimeeMinutes,
                    },
                    Exercices = (m.Exercices ?? new()).Select(e => new Exercice
                    {
                        Enonce = e.Enonce ?? "",
                        TypeReponse = ParseEnum(e.TypeReponse, TypeReponse.TexteLibre),
                        ReponseCorrecte = e.ReponseCorrecte,
                        CorrectionAuto = !string.IsNullOrWhiteSpace(e.ReponseCorrecte),
                        Points = e.Points <= 0 ? 10 : e.Points,
                    }).ToList(),
                    Examens = (m.Examens ?? new()).Select(ex => new Examen
                    {
                        Titre = ex.Titre ?? "Quiz",
                        DureeMinutes = ex.DureeMinutes <= 0 ? 10 : ex.DureeMinutes,
                        NoteMinPassage = ex.NoteMinPassage <= 0 ? 60 : ex.NoteMinPassage,
                        TentativesMax = ex.TentativesMax <= 0 ? 3 : ex.TentativesMax,
                        IsActif = true,
                        Questions = (ex.Questions ?? new()).Select((q, i) => new QuestionQuiz
                        {
                            Enonce = q.Enonce ?? "",
                            Choix = JsonSerializer.Serialize(q.Choix ?? new()),
                            BonneReponse = q.BonneReponse ?? "",
                            Explication = q.Explication,
                            Points = q.Points <= 0 ? 1 : q.Points,
                            Ordre = i + 1,
                        }).ToList(),
                    }).ToList(),
                }).ToList(),
            };

            context.Formations.Add(formation);
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Formations de démonstration créées ({Count})", seed.Formations.Count);
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct =>
        Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : fallback;

    private static SeedRoot? LoadSeedJson()
    {
        var assembly = typeof(DbSeeder).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("formations.seed.json", StringComparison.OrdinalIgnoreCase));
        if (resourceName is null) return null;

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null) return null;
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<SeedRoot>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    // ── Modèles de désérialisation du JSON de seed ────────────────────────────
    private sealed class SeedRoot { public List<SeedFormation>? Formations { get; set; } }

    private sealed class SeedFormation
    {
        public string? Titre { get; set; }
        public string? Description { get; set; }
        public string? Categorie { get; set; }
        public string? Niveau { get; set; }
        public int DureeHeures { get; set; }
        public decimal Prix { get; set; }
        public string? Statut { get; set; }
        public string? Objectifs { get; set; }
        public string? Prerequis { get; set; }
        public List<SeedModule>? Modules { get; set; }
    }

    private sealed class SeedModule
    {
        public string? Titre { get; set; }
        public int Ordre { get; set; }
        public string? Description { get; set; }
        public SeedCours? Cours { get; set; }
        public List<SeedExercice>? Exercices { get; set; }
        public List<SeedExamen>? Examens { get; set; }
    }

    private sealed class SeedCours
    {
        public string? Contenu { get; set; }
        public string? TextePourAvatar { get; set; }
        public string? VideoUrl { get; set; }
        public string? PdfUrl { get; set; }
        public int DureeEstimeeMinutes { get; set; }
    }

    private sealed class SeedExercice
    {
        public string? Enonce { get; set; }
        public string? TypeReponse { get; set; }
        public string? ReponseCorrecte { get; set; }
        public int Points { get; set; }
    }

    private sealed class SeedExamen
    {
        public string? Titre { get; set; }
        public int DureeMinutes { get; set; }
        public decimal NoteMinPassage { get; set; }
        public int TentativesMax { get; set; }
        public List<SeedQuestion>? Questions { get; set; }
    }

    private sealed class SeedQuestion
    {
        public string? Enonce { get; set; }
        public List<string>? Choix { get; set; }
        public string? BonneReponse { get; set; }
        public string? Explication { get; set; }
        public int Points { get; set; }
    }
}
