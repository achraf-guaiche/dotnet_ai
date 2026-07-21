using FluentValidation;

namespace Formations.Application.Categories.DTOs;

public record CategorieDto(int Id, string Nom, string? Description, string? Icone, int NbFormations);

public record CreateCategorieRequest(string Nom, string? Description, string? Icone);

public record UpdateCategorieRequest(string Nom, string? Description, string? Icone);

public class CreateCategorieValidator : AbstractValidator<CreateCategorieRequest>
{
    public CreateCategorieValidator()
    {
        RuleFor(x => x.Nom).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Icone).MaximumLength(100);
    }
}
