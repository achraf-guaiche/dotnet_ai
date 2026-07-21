using Formations.Domain.Common;

namespace Formations.Domain.Entities;

public class Formateur : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public string? Bio { get; set; }
    public string? Specialites { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<FormationFormateur> FormationFormateurs { get; set; } = [];
}
