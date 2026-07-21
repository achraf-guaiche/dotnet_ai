using Microsoft.AspNetCore.Identity;

namespace Formations.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    public ICollection<Inscription> Inscriptions { get; set; } = [];
    public ICollection<ResultatExamen> ResultatsExamens { get; set; } = [];
    public ICollection<SessionAvatarIA> SessionsAvatarIA { get; set; } = [];
    public ICollection<ActivityLog> ActivityLogs { get; set; } = [];
    public Formateur? Formateur { get; set; }
}
