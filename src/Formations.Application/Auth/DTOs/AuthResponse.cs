namespace Formations.Application.Auth.DTOs;

public record AuthResponse(
    string UserId,
    string Email,
    string FullName,
    IList<string> Roles,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry
);

public record RefreshRequest(string AccessToken, string RefreshToken);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Email, string Token, string NewPassword, string ConfirmPassword);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);

public record UpdateProfileRequest(string FullName, string? Bio, string? AvatarUrl);

public record UserProfileResponse(
    string Id,
    string Email,
    string FullName,
    string? AvatarUrl,
    string? Bio,
    IList<string> Roles,
    bool IsActive,
    DateTime CreatedAt
);
