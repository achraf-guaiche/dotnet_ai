using Formations.Domain.Common;

namespace Formations.Domain.Entities;

public class Module : BaseEntity
{
    public int FormationId { get; set; }
    public Formation Formation { get; set; } = null!;

    public string Titre { get; set; } = string.Empty;
    public int Ordre { get; set; }
    public string? Description { get; set; }
    public bool IsPublie { get; set; } = false;

    public Cours? Cours { get; set; }
    public ICollection<Exercice> Exercices { get; set; } = [];
    public ICollection<Examen> Examens { get; set; } = [];
    public ICollection<SessionAvatarIA> SessionsAvatarIA { get; set; } = [];
}
