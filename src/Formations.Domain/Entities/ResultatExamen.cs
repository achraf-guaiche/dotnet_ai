using Formations.Domain.Common;

namespace Formations.Domain.Entities;

public class ResultatExamen : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int ExamenId { get; set; }
    public Examen Examen { get; set; } = null!;

    public decimal Score { get; set; }
    public int TentativeNumero { get; set; }
    public DateTime DatePassage { get; set; } = DateTime.UtcNow;
    public bool IsReussi { get; set; }
    public int DureeSecondes { get; set; }
    public string? ReponsesJson { get; set; }
}
