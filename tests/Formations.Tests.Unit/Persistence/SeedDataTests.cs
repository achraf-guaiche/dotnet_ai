using System.Text.Json;
using FluentAssertions;
using Formations.Infrastructure.Persistence;
using Xunit;

namespace Formations.Tests.Unit.Persistence;

// Vérifie que le JSON de seed est bien embarqué dans l'assembly et structurellement valide.
public class SeedDataTests
{
    private static JsonElement LoadSeedRoot()
    {
        var assembly = typeof(DbSeeder).Assembly;
        var name = assembly.GetManifestResourceNames()
            .Single(n => n.EndsWith("formations.seed.json", StringComparison.OrdinalIgnoreCase));
        using var stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return JsonDocument.Parse(reader.ReadToEnd()).RootElement.Clone();
    }

    [Fact]
    public void SeedJson_IsEmbedded_AndParsable()
    {
        var root = LoadSeedRoot();
        root.TryGetProperty("formations", out var formations).Should().BeTrue();
        formations.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public void SeedJson_EachFormation_HasCategoryAndModulesWithContent()
    {
        var root = LoadSeedRoot();
        var formations = root.GetProperty("formations").EnumerateArray().ToList();

        formations.All(f =>
            f.GetProperty("titre").GetString()!.Length > 0 &&
            f.GetProperty("categorie").GetString()!.Length > 0).Should().BeTrue();

        // Au moins une formation possède un module complet : cours + exercice + quiz avec question.
        static bool HasFullModule(JsonElement f) =>
            f.GetProperty("modules").EnumerateArray().Any(m =>
                m.TryGetProperty("cours", out _) &&
                m.TryGetProperty("exercices", out var ex) && ex.GetArrayLength() > 0 &&
                m.TryGetProperty("examens", out var qz) && qz.GetArrayLength() > 0 &&
                qz[0].GetProperty("questions").GetArrayLength() > 0);

        formations.Any(HasFullModule).Should().BeTrue();
    }

    [Fact]
    public void SeedJson_QuizQuestions_HaveChoicesAndAValidAnswer()
    {
        var root = LoadSeedRoot();
        foreach (var f in root.GetProperty("formations").EnumerateArray())
        foreach (var m in f.GetProperty("modules").EnumerateArray())
        {
            if (!m.TryGetProperty("examens", out var examens)) continue;
            foreach (var e in examens.EnumerateArray())
            foreach (var q in e.GetProperty("questions").EnumerateArray())
            {
                var choix = q.GetProperty("choix").EnumerateArray().Select(c => c.GetString()).ToList();
                var bonne = q.GetProperty("bonneReponse").GetString();
                choix.Should().HaveCountGreaterThanOrEqualTo(2);
                choix.Should().Contain(bonne, "la bonne réponse doit figurer parmi les choix");
            }
        }
    }
}
