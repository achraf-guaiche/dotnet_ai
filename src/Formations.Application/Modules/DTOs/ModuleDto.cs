using FluentValidation;

namespace Formations.Application.Modules.DTOs;

public record ModuleDto(
    int Id,
    int FormationId,
    string FormationTitre,
    string Titre,
    int Ordre,
    string? Description,
    bool IsPublie,
    bool HasCours,
    int NbExercices,
    int NbExamens
);

public record CoursDto(
    int Id,
    int ModuleId,
    string? Contenu,
    string? VideoUrl,
    string? PdfUrl,
    string? TextePourAvatar,
    int DureeEstimeeMinutes
);

public record ExerciceDto(
    int Id,
    int ModuleId,
    string Enonce,
    string TypeReponse,
    bool CorrectionAuto,
    string? Instructions,
    int Points
);

public record ExamenDto(
    int Id,
    int ModuleId,
    string Titre,
    string? Description,
    int DureeMinutes,
    decimal NoteMinPassage,
    int TentativesMax,
    int NbQuestions,
    bool IsActif
);

public record QuestionQuizDto(
    int Id,
    int ExamenId,
    string Enonce,
    List<string> Choix,
    string BonneReponse,
    string? Explication,
    int Points,
    int Ordre
);

public record CreateModuleRequest(int FormationId, string Titre, int Ordre, string? Description);
public record UpdateModuleRequest(string Titre, int Ordre, string? Description, bool IsPublie);

public record CreateCoursRequest(int ModuleId, string? Contenu, string? VideoUrl, string? PdfUrl, string? TextePourAvatar, int DureeEstimeeMinutes);
public record UpdateCoursRequest(string? Contenu, string? VideoUrl, string? PdfUrl, string? TextePourAvatar, int DureeEstimeeMinutes);

public record CreateExerciceRequest(int ModuleId, string Enonce, string TypeReponse, bool CorrectionAuto, string? ReponseCorrecte, string? Instructions, int Points);

public record CreateExamenRequest(int ModuleId, string Titre, string? Description, int DureeMinutes, decimal NoteMinPassage, int TentativesMax);
public record UpdateExamenRequest(string Titre, string? Description, int DureeMinutes, decimal NoteMinPassage, int TentativesMax, bool IsActif);

public record CreateQuestionQuizRequest(int ExamenId, string Enonce, List<string> Choix, string BonneReponse, string? Explication, int Points, int Ordre);

public record ExamenAttemptRequest(int ExamenId, Dictionary<int, string> Reponses, int DureeSecondes);
public record ExamenAttemptResult(decimal Score, bool IsReussi, int CorrectCount, int TotalQuestions, int TentativeNumero, Dictionary<int, bool> DetailsParQuestion);

public class CreateModuleValidator : AbstractValidator<CreateModuleRequest>
{
    public CreateModuleValidator()
    {
        RuleFor(x => x.FormationId).GreaterThan(0);
        RuleFor(x => x.Titre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Ordre).GreaterThanOrEqualTo(0);
    }
}
