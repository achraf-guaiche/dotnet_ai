using FluentValidation;

namespace Formations.Application.Auth.DTOs;

public record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string ConfirmPassword,
    string Role = RegisterRequest.RoleApprenant
)
{
    public const string RoleApprenant = "Apprenant";
    public const string RoleFormateur = "Formateur";

    /// <summary>Seuls ces rôles peuvent être choisis à l'inscription (Admin exclu).</summary>
    public static readonly string[] RolesAutorises = { RoleApprenant, RoleFormateur };
}

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Role)
            .Must(r => RegisterRequest.RolesAutorises.Contains(r))
            .WithMessage("Le rôle doit être « Apprenant » ou « Formateur ».");
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Le mot de passe doit contenir au moins une majuscule.")
            .Matches("[0-9]").WithMessage("Le mot de passe doit contenir au moins un chiffre.");
        RuleFor(x => x.ConfirmPassword).Equal(x => x.Password).WithMessage("Les mots de passe ne correspondent pas.");
    }
}
