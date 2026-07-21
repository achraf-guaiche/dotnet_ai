using Blazored.LocalStorage;

namespace Formations.Web.Services;

public class AuthWebService
{
    private readonly IApiClient _api;
    private readonly ILocalStorageService _localStorage;
    private readonly ApiAuthStateProvider _authStateProvider;

    public AuthWebService(IApiClient api, ILocalStorageService localStorage, ApiAuthStateProvider authStateProvider)
    {
        _api = api;
        _localStorage = localStorage;
        _authStateProvider = (ApiAuthStateProvider)authStateProvider;
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        var result = await _api.PostAsync<object, LoginResult>("api/auth/login",
            new { Email = email, Password = password });

        if (result is null) return false;

        await _localStorage.SetItemAsStringAsync("access_token", result.AccessToken);
        await _localStorage.SetItemAsStringAsync("refresh_token", result.RefreshToken);
        _authStateProvider.NotifyAuthStateChanged();
        return true;
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync("access_token");
        await _localStorage.RemoveItemAsync("refresh_token");
        _authStateProvider.NotifyAuthStateChanged();
    }

    public async Task<bool> RegisterAsync(string fullName, string email, string password, string confirmPassword)
    {
        var result = await _api.PostAsync<object, LoginResult>("api/auth/register",
            new { FullName = fullName, Email = email, Password = password, ConfirmPassword = confirmPassword });

        if (result is null) return false;

        await _localStorage.SetItemAsStringAsync("access_token", result.AccessToken);
        await _localStorage.SetItemAsStringAsync("refresh_token", result.RefreshToken);
        _authStateProvider.NotifyAuthStateChanged();
        return true;
    }
}

public record LoginResult(string AccessToken, string RefreshToken, string UserId, string FullName, IList<string> Roles);
