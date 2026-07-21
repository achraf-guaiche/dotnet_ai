using Formations.Domain.Common;

namespace Formations.Domain.Entities;

public class ActivityLog : BaseEntity
{
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
