using Formations.Domain.Common;

namespace Formations.Domain.Entities;

public class Examen : BaseEntity
{
    public int ModuleId { get; set; }
    public Module Module { get; set; } = null!;

    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DureeMinutes { get; set; } = 30;
    public decimal NoteMinPassage { get; set; } = 60;
    public int TentativesMax { get; set; } = 3;
    public bool IsActif { get; set; } = true;

    public ICollection<QuestionQuiz> Questions { get; set; } = [];
    public ICollection<ResultatExamen> Resultats { get; set; } = [];
}
