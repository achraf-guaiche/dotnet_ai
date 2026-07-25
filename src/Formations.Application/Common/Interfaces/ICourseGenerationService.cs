namespace Formations.Application.Common.Interfaces;

/// <summary>
/// Génère le contenu pédagogique d'un module (cours, script pour le prof IA vidéo, quiz)
/// à partir d'un prompt libre fourni par le formateur, via le LLM configuré.
/// </summary>
public interface ICourseGenerationService
{
    Task<GeneratedCourse> GenerateModuleContentAsync(
        string prompt,
        string moduleTitre,
        string formationTitre,
        CancellationToken ct = default);
}

public record GeneratedCourse(
    string Contenu,              // HTML du cours affiché à l'apprenant
    string TextePourAvatar,     // script récité par le prof IA vidéo interactif
    int DureeEstimeeMinutes,
    List<GeneratedQuestion> Quiz,
    List<GeneratedExercice> Exercices);

public record GeneratedQuestion(
    string Enonce,
    List<string> Choix,
    string BonneReponse,
    string? Explication,
    int Points);

public record GeneratedExercice(
    string Enonce,
    string TypeReponse,         // TexteLibre | ChoixMultiple | VraiOuFaux | FichierUpload
    string? ReponseCorrecte,   // si fournie → correction automatique
    int Points);
