using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Formations.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Formations.Infrastructure.Services;

public class AvatarIAService : IAvatarIAService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<AvatarIAService> _logger;

    public AvatarIAService(HttpClient httpClient, IConfiguration config, ILogger<AvatarIAService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;

        IsMockMode = _config["AvatarIA:Provider"]?.ToLower() == "mock"
            || string.IsNullOrEmpty(_config["AvatarIA:ApiKey"]);
    }

    public bool IsMockMode { get; }

    public async Task<AvatarSessionResult> CreateSessionAsync(string textePresentation, CancellationToken ct = default)
    {
        if (IsMockMode)
        {
            _logger.LogInformation("[MOCK] Création session avatar IA simulée");
            return new AvatarSessionResult(
                SessionId: $"mock-session-{Guid.NewGuid():N}",
                StreamUrl: "/mock-avatar-stream",
                EmbedToken: "mock-embed-token");
        }

        var provider = _config["AvatarIA:Provider"]?.ToLower();
        return provider switch
        {
            "anam" => await CreateAnamSessionAsync(textePresentation, ct),
            "heygen" => await CreateHeyGenSessionAsync(textePresentation, ct),
            _ => throw new InvalidOperationException($"Provider avatar inconnu : {provider}")
        };
    }

    private async Task<AvatarSessionResult> CreateAnamSessionAsync(string textePresentation, CancellationToken ct)
    {
        var request = new
        {
            persona_id = _config["AvatarIA:PersonaId"],
            initial_message = textePresentation
        };

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config["AvatarIA:ApiKey"]}");

        var response = await _httpClient.PostAsJsonAsync(
            $"{_config["AvatarIA:BaseUrl"]}/sessions/start", request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return new AvatarSessionResult(
            SessionId: result.GetProperty("session_id").GetString()!,
            StreamUrl: result.GetProperty("stream_url").GetString()!,
            EmbedToken: result.TryGetProperty("embed_token", out var t) ? t.GetString()! : "");
    }

    private async Task<AvatarSessionResult> CreateHeyGenSessionAsync(string textePresentation, CancellationToken ct)
    {
        var request = new
        {
            quality = "high",
            avatar_name = _config["AvatarIA:AvatarId"],
            voice = new { rate = 1.0 }
        };

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _config["AvatarIA:ApiKey"]);

        var response = await _httpClient.PostAsJsonAsync("https://api.heygen.com/v1/streaming.new", request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var data = result.GetProperty("data");

        return new AvatarSessionResult(
            SessionId: data.GetProperty("session_id").GetString()!,
            StreamUrl: data.GetProperty("url").GetString()!,
            EmbedToken: data.TryGetProperty("access_token", out var token) ? token.GetString()! : "");
    }

    public async Task<string> GetAIResponseAsync(
        string userMessage,
        string moduleContext,
        IEnumerable<ChatMessage> history,
        CancellationToken ct = default)
    {
        if (IsMockMode)
        {
            _logger.LogInformation("[MOCK] Réponse IA simulée pour : {Message}", userMessage);
            await Task.Delay(500, ct);
            return $"[Réponse simulée] J'ai bien reçu votre question : « {userMessage} ». " +
                   "Dans un environnement de production avec la vraie clé API Claude, " +
                   "je répondrais en me basant sur le contexte du module.";
        }

        var apiKey = _config["LLM:ApiKey"];
        var model = _config["LLM:Model"] ?? "claude-sonnet-4-6";

        var messages = new List<object>();
        foreach (var msg in history.TakeLast(10))
            messages.Add(new { role = msg.Role, content = msg.Content });
        messages.Add(new { role = "user", content = userMessage });

        var requestBody = new
        {
            model,
            max_tokens = 1024,
            system = $"""
                Tu es un formateur IA expert et pédagogue. Tu réponds aux questions des apprenants
                sur le module suivant : {moduleContext}

                Règles :
                - Réponds en français, de façon claire et concise
                - Adapte ton niveau à l'apprenant
                - Si la question est hors sujet, redirige poliment vers le contenu du module
                - Tes réponses seront lues par un avatar IA, donc évite les formatages markdown complexes
                """,
            messages
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return result.GetProperty("content")[0].GetProperty("text").GetString() ?? "Je n'ai pas pu générer une réponse.";
    }

    public async Task<string> TextToAvatarSpeechAsync(string text, string sessionId, CancellationToken ct = default)
    {
        if (IsMockMode)
            return $"/mock-audio/{Guid.NewGuid():N}.mp3";

        var provider = _config["AvatarIA:Provider"]?.ToLower();

        if (provider == "anam")
        {
            var request = new { session_id = sessionId, text };
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config["AvatarIA:ApiKey"]}");
            var response = await _httpClient.PostAsJsonAsync($"{_config["AvatarIA:BaseUrl"]}/sessions/speak", request, ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            return result.TryGetProperty("audio_url", out var url) ? url.GetString()! : "";
        }

        if (provider == "heygen")
        {
            var request = new { session_id = sessionId, text, task_type = "repeat" };
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _config["AvatarIA:ApiKey"]);
            var response = await _httpClient.PostAsJsonAsync("https://api.heygen.com/v1/streaming.task", request, ct);
            response.EnsureSuccessStatusCode();
            return sessionId;
        }

        return "";
    }

    public async Task EndSessionAsync(string sessionId, CancellationToken ct = default)
    {
        if (IsMockMode) return;

        var provider = _config["AvatarIA:Provider"]?.ToLower();
        _httpClient.DefaultRequestHeaders.Clear();

        if (provider == "anam")
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config["AvatarIA:ApiKey"]}");
            await _httpClient.PostAsJsonAsync($"{_config["AvatarIA:BaseUrl"]}/sessions/{sessionId}/end", new { }, ct);
        }
        else if (provider == "heygen")
        {
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _config["AvatarIA:ApiKey"]);
            await _httpClient.PostAsJsonAsync("https://api.heygen.com/v1/streaming.stop", new { session_id = sessionId }, ct);
        }
    }
}
