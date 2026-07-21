using FluentValidation;

namespace Formations.Application.Formateurs.DTOs;

public record FormateurDto(
    int Id,
    string UserId,
    string UserFullName,
    string UserEmail,
    string? Bio,
    string? Specialites,
    string? PhotoUrl,
    bool IsActive,
    int NbFormations
);

public record CreateFormateurRequest(string UserId, string? Bio, string? Specialites, string? PhotoUrl);

public record UpdateFormateurRequest(string? Bio, string? Specialites, string? PhotoUrl, bool IsActive);

public class CreateFormateurValidator : AbstractValidator<CreateFormateurRequest>
{
    public CreateFormateurValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Bio).MaximumLength(2000);
        RuleFor(x => x.Specialites).MaximumLength(500);
    }
}
