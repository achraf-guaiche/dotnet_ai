namespace Formations.Domain.Entities;

public class FormationFormateur
{
    public int FormationId { get; set; }
    public Formation Formation { get; set; } = null!;

    public int FormateurId { get; set; }
    public Formateur Formateur { get; set; } = null!;

    public bool IsLead { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
