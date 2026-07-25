using Formations.Domain.Common;

namespace Formations.Domain.Entities;

// Tentative d'un apprenant sur un exercice (persistée pour consultation / reprise / progression).
public class ResultatExercice : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int ExerciceId { get; set; }
    public Exercice Exercice { get; set; } = null!;

    public string? Reponse { get; set; }
    public bool IsCorrect { get; set; }
    public int TentativeNumero { get; set; }
    public DateTime DatePassage { get; set; } = DateTime.UtcNow;
}
