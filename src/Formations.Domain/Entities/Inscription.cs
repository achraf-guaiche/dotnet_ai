using Formations.Domain.Common;
using Formations.Domain.Enums;

namespace Formations.Domain.Entities;

public class Inscription : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int FormationId { get; set; }
    public Formation Formation { get; set; } = null!;

    public DateTime DateInscription { get; set; } = DateTime.UtcNow;
    public StatutInscription Statut { get; set; } = StatutInscription.EnCours;
    public decimal ProgressionPourcent { get; set; }
    public DateTime? DateTerminee { get; set; }
    public string? NumeroCertificat { get; set; }
    public int ModulesCompletes { get; set; }
    public int TotalModules { get; set; }
}
