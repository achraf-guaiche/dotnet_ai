namespace Formations.Application.Admin.DTOs;

public record DashboardStats(
    int TotalFormations,
    int TotalApprenants,
    int TotalFormateurs,
    int TotalInscriptions,
    decimal TauxCompletionMoyen,
    int FormationsPubliees,
    List<FormationStatDto> TopFormations,
    List<InscriptionRecenteDto> RecentesInscriptions
);

public record FormationStatDto(int Id, string Titre, int NbInscrits, decimal TauxCompletion);

public record InscriptionRecenteDto(string UserFullName, string FormationTitre, DateTime Date);

public record AdminUserDto(
    string Id,
    string Email,
    string FullName,
    List<string> Roles,
    bool IsActive,
    DateTime CreatedAt,
    int NbInscriptions
);

public record ChangeUserRoleRequest(string Role);
public record ToggleUserActiveRequest(bool IsActive);

public record ActivityLogDto(
    int Id,
    string? UserFullName,
    string Action,
    string? Details,
    string? IpAddress,
    DateTime CreatedAt
);
