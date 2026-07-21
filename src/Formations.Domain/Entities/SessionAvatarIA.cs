using Formations.Domain.Common;

namespace Formations.Domain.Entities;

public class SessionAvatarIA : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int ModuleId { get; set; }
    public Module Module { get; set; } = null!;

    public string Historique { get; set; } = "[]";
    public DateTime DateSession { get; set; } = DateTime.UtcNow;
    public DateTime? DateFin { get; set; }
    public int DureeSecondes { get; set; }
    public string? SessionExterneId { get; set; }
}
