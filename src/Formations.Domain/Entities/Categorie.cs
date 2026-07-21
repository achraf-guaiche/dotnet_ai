using Formations.Domain.Common;

namespace Formations.Domain.Entities;

public class Categorie : BaseEntity
{
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icone { get; set; }

    public ICollection<Formation> Formations { get; set; } = [];
}
