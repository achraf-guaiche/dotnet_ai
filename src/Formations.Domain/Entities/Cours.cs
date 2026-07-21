using Formations.Domain.Common;

namespace Formations.Domain.Entities;

public class Cours : BaseEntity
{
    public int ModuleId { get; set; }
    public Module Module { get; set; } = null!;

    public string? Contenu { get; set; }
    public string? VideoUrl { get; set; }
    public string? PdfUrl { get; set; }
    public string? TextePourAvatar { get; set; }
    public int DureeEstimeeMinutes { get; set; }
}
