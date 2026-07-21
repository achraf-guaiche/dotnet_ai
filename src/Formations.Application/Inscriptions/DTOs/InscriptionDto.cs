using Formations.Domain.Enums;

namespace Formations.Application.Inscriptions.DTOs;

public record InscriptionDto(
    int Id,
    string UserId,
    string UserFullName,
    int FormationId,
    string FormationTitre,
    string? FormationImage,
    StatutInscription Statut,
    decimal ProgressionPourcent,
    int ModulesCompletes,
    int TotalModules,
    DateTime DateInscription,
    DateTime? DateTerminee,
    string? NumeroCertificat
);

public record CreateInscriptionRequest(int FormationId);

public record UpdateProgressRequest(int ModuleId, bool IsComplete);

public record AdminUpdateInscriptionRequest(StatutInscription Statut, decimal ProgressionPourcent);
