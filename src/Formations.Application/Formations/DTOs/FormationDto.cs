using FluentValidation;
using Formations.Domain.Enums;

namespace Formations.Application.Formations.DTOs;

public record FormationDto(
    int Id,
    string Titre,
    string? Description,
    int CategorieId,
    string CategorieNom,
    NiveauFormation Niveau,
    string NiveauLabel,
    int DureeHeures,
    StatutFormation Statut,
    string? ImageUrl,
    string? Objectifs,
    string? Prerequis,
    decimal Prix,
    int NbModules,
    int NbInscrits,
    List<FormateurSummaryDto> Formateurs,
    DateTime CreatedAt
);

public record FormateurSummaryDto(int Id, string FullName, string? PhotoUrl);

public record FormationSummaryDto(
    int Id,
    string Titre,
    string? ImageUrl,
    string CategorieNom,
    string NiveauLabel,
    int DureeHeures,
    decimal Prix,
    int NbInscrits,
    StatutFormation Statut
);

public record CreateFormationRequest(
    string Titre,
    string? Description,
    int CategorieId,
    NiveauFormation Niveau,
    int DureeHeures,
    string? ImageUrl,
    string? Objectifs,
    string? Prerequis,
    decimal Prix,
    List<int>? FormateurIds
);

public record UpdateFormationRequest(
    string Titre,
    string? Description,
    int CategorieId,
    NiveauFormation Niveau,
    int DureeHeures,
    StatutFormation Statut,
    string? ImageUrl,
    string? Objectifs,
    string? Prerequis,
    decimal Prix,
    List<int>? FormateurIds
);

public class CreateFormationValidator : AbstractValidator<CreateFormationRequest>
{
    public CreateFormationValidator()
    {
        RuleFor(x => x.Titre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(5000);
        RuleFor(x => x.CategorieId).GreaterThan(0);
        RuleFor(x => x.DureeHeures).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Prix).GreaterThanOrEqualTo(0);
    }
}
