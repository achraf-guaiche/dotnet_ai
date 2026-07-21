using Formations.Domain.Common;

namespace Formations.Domain.Entities;

public class QuestionQuiz : BaseEntity
{
    public int ExamenId { get; set; }
    public Examen Examen { get; set; } = null!;

    public string Enonce { get; set; } = string.Empty;
    public string Choix { get; set; } = "[]";
    public string BonneReponse { get; set; } = string.Empty;
    public string? Explication { get; set; }
    public int Points { get; set; } = 1;
    public int Ordre { get; set; }
}
