using Blazored.LocalStorage;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Formations.Web.Services;

public interface IApiClient
{
    Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default);
    Task<TResult?> PostAsync<TBody, TResult>(string endpoint, TBody body, CancellationToken ct = default);
    Task<TResult?> PutAsync<TBody, TResult>(string endpoint, TBody body, CancellationToken ct = default);
    Task<bool> DeleteAsync(string endpoint, CancellationToken ct = default);
    Task<byte[]?> GetBytesAsync(string endpoint, CancellationToken ct = default);
}

public class ApiClient : IApiClient
{
    private readonly IHttpClientFactory _factory;
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(IHttpClientFactory factory, ILocalStorageService localStorage, ILogger<ApiClient> logger)
    {
        _factory = factory;
        _localStorage = localStorage;
        _logger = logger;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient("FormationsApi");
        var token = await _localStorage.GetItemAsStringAsync("access_token");
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default)
    {
        try
        {
            var client = await CreateAuthenticatedClientAsync();
            return await client.GetFromJsonAsync<T>(endpoint, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET {Endpoint} failed", endpoint);
            return default;
        }
    }

    public async Task<TResult?> PostAsync<TBody, TResult>(string endpoint, TBody body, CancellationToken ct = default)
    {
        try
        {
            var client = await CreateAuthenticatedClientAsync();
            var response = await client.PostAsJsonAsync(endpoint, body, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResult>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST {Endpoint} failed", endpoint);
            return default;
        }
    }

    public async Task<TResult?> PutAsync<TBody, TResult>(string endpoint, TBody body, CancellationToken ct = default)
    {
        try
        {
            var client = await CreateAuthenticatedClientAsync();
            var response = await client.PutAsJsonAsync(endpoint, body, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResult>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PUT {Endpoint} failed", endpoint);
            return default;
        }
    }

    public async Task<bool> DeleteAsync(string endpoint, CancellationToken ct = default)
    {
        try
        {
            var client = await CreateAuthenticatedClientAsync();
            var response = await client.DeleteAsync(endpoint, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DELETE {Endpoint} failed", endpoint);
            return false;
        }
    }

    public async Task<byte[]?> GetBytesAsync(string endpoint, CancellationToken ct = default)
    {
        try
        {
            var client = await CreateAuthenticatedClientAsync();
            return await client.GetByteArrayAsync(endpoint, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET bytes {Endpoint} failed", endpoint);
            return null;
        }
    }
}
