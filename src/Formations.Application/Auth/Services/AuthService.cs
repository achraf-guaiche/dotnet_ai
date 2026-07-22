using Formations.Application.Auth.DTOs;
using Formations.Application.Common.Interfaces;
using Formations.Application.Common.Models;
using Formations.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Formations.Application.Auth.Services;

public class AuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;
    private readonly IApplicationDbContext _context;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IJwtService jwtService,
        IEmailService emailService,
        IApplicationDbContext context)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _emailService = emailService;
        _context = context;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (await _userManager.FindByEmailAsync(request.Email) is not null)
            return Result<AuthResponse>.Failure("Un compte existe déjà avec cet email.");

        // Le rôle vient du client : on n'accepte QUE la liste blanche (jamais Admin).
        var roleDemande = RegisterRequest.RolesAutorises.Contains(request.Role)
            ? request.Role
            : RegisterRequest.RoleApprenant;

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            EmailConfirmed = true
        };

        var identityResult = await _userManager.CreateAsync(user, request.Password);
        if (!identityResult.Succeeded)
            return Result<AuthResponse>.Failure(identityResult.Errors.Select(e => e.Description));

        await _userManager.AddToRoleAsync(user, roleDemande);

        // Un formateur a besoin d'une fiche Formateur pour être rattaché à des formations.
        if (roleDemande == RegisterRequest.RoleFormateur)
        {
            _context.Formateurs.Add(new Formateur { UserId = user.Id });
            await _context.SaveChangesAsync(ct);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtService.GenerateAccessToken(user, roles);
        var refreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(30);
        await _userManager.UpdateAsync(user);

        await _emailService.SendWelcomeAsync(user.Email!, user.FullName, ct);
        await LogActivityAsync(user.Id, "Register", $"Nouvelle inscription ({roleDemande})", ct);

        return Result<AuthResponse>.Success(new AuthResponse(
            user.Id, user.Email!, user.FullName, roles,
            accessToken, refreshToken, DateTime.UtcNow.AddHours(1)));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
            return Result<AuthResponse>.Failure("Email ou mot de passe incorrect.");

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
            return Result<AuthResponse>.Failure("Email ou mot de passe incorrect.");

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _jwtService.GenerateAccessToken(user, roles);
        var refreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(30);
        await _userManager.UpdateAsync(user);

        await LogActivityAsync(user.Id, "Login", null, ct);

        return Result<AuthResponse>.Success(new AuthResponse(
            user.Id, user.Email!, user.FullName, roles,
            accessToken, refreshToken, DateTime.UtcNow.AddHours(1)));
    }

    public async Task<Result<AuthResponse>> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        var userId = _jwtService.GetUserIdFromExpiredToken(request.AccessToken);
        if (userId is null)
            return Result<AuthResponse>.Failure("Token invalide.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiry < DateTime.UtcNow)
            return Result<AuthResponse>.Failure("Refresh token invalide ou expiré.");

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _jwtService.GenerateAccessToken(user, roles);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(30);
        await _userManager.UpdateAsync(user);

        return Result<AuthResponse>.Success(new AuthResponse(
            user.Id, user.Email!, user.FullName, roles,
            newAccessToken, newRefreshToken, DateTime.UtcNow.AddHours(1)));
    }

    public async Task<Result> ForgotPasswordAsync(string email, string baseUrl, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null) return Result.Success();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
        var resetLink = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(email)}&token={encodedToken}";

        await _emailService.SendPasswordResetAsync(user.Email!, resetLink, ct);
        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null) return Result.Failure("Utilisateur introuvable.");

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
            return Result.Failure(result.Errors.Select(e => e.Description));

        user.RefreshToken = null;
        await _userManager.UpdateAsync(user);
        return Result.Success();
    }

    public async Task<Result<UserProfileResponse>> GetProfileAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return Result<UserProfileResponse>.Failure("Utilisateur introuvable.");

        var roles = await _userManager.GetRolesAsync(user);
        return Result<UserProfileResponse>.Success(new UserProfileResponse(
            user.Id, user.Email!, user.FullName, user.AvatarUrl, user.Bio, roles, user.IsActive, user.CreatedAt));
    }

    public async Task<Result<UserProfileResponse>> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return Result<UserProfileResponse>.Failure("Utilisateur introuvable.");

        user.FullName = request.FullName;
        user.Bio = request.Bio;
        user.AvatarUrl = request.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;

        await _userManager.UpdateAsync(user);
        var roles = await _userManager.GetRolesAsync(user);

        return Result<UserProfileResponse>.Success(new UserProfileResponse(
            user.Id, user.Email!, user.FullName, user.AvatarUrl, user.Bio, roles, user.IsActive, user.CreatedAt));
    }

    private async Task LogActivityAsync(string userId, string action, string? details, CancellationToken ct)
    {
        _context.ActivityLogs.Add(new ActivityLog { UserId = userId, Action = action, Details = details });
        await _context.SaveChangesAsync(ct);
    }
}
