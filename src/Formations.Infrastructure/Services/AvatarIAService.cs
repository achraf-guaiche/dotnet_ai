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
        try
        {
            return provider switch
            {
                "anam" => await CreateAnamSessionAsync(textePresentation, ct),
                "heygen" => await CreateHeyGenSessionAsync(textePresentation, ct),
                _ => throw new InvalidOperationException($"Provider avatar inconnu : {provider}")
            };
        }
        catch (Exception ex)
        {
            // Dégradation gracieuse : si le fournisseur échoue (clé invalide, quota, panne),
            // on retombe sur une session simulée plutôt que de renvoyer une 500 à l'apprenant.
            _logger.LogError(ex, "Échec de la création de session avatar ({Provider}) — repli en mode simulé", provider);
            return new AvatarSessionResult(
                SessionId: $"fallback-session-{Guid.NewGuid():N}",
                StreamUrl: null,
                EmbedToken: "");
        }
    }

    // Anam n'expose pas de « start session » : on échange la clé API contre un
    // session token court (POST /v1/auth/session-token) que le SDK JS utilise côté navigateur.
    private async Task<AvatarSessionResult> CreateAnamSessionAsync(string textePresentation, CancellationToken ct)
    {
        var personaId = Cfg("AvatarIA:PersonaId");
        object personaConfig = personaId is not null
            ? new { id = personaId }
            : new { name = "Formateur", systemPrompt = textePresentation };

        var request = new { personaConfig };

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config["AvatarIA:ApiKey"]}");

        var baseUrl = (Cfg("AvatarIA:BaseUrl") ?? "https://api.anam.ai/v1").TrimEnd('/');
        var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/auth/session-token", request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Anam a répondu {Status} : {Body}", (int)response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode();
        }

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var sessionToken = result.TryGetProperty("sessionToken", out var st) ? st.GetString()
            : result.TryGetProperty("session_token", out var st2) ? st2.GetString()
            : null;

        if (string.IsNullOrEmpty(sessionToken))
            throw new InvalidOperationException("Anam n'a pas renvoyé de sessionToken.");

        // Pas d'URL de stream : le navigateur ouvre le flux via le SDK Anam avec ce token.
        return new AvatarSessionResult(
            SessionId: $"anam-{Guid.NewGuid():N}",
            StreamUrl: null,
            EmbedToken: sessionToken);
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
        var provider = (_config["LLM:Provider"] ?? "claude").ToLowerInvariant();
        var apiKey = _config["LLM:ApiKey"];

        // Mode dev / mock : aucune clé LLM configurée → réponse simulée (aucun appel externe).
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogInformation("[MOCK LLM:{Provider}] Réponse IA simulée pour : {Message}", provider, userMessage);
            await Task.Delay(500, ct);
            return $"[Réponse simulée · fournisseur « {provider} »] J'ai bien reçu votre question : " +
                   $"« {userMessage} ». Avec une vraie clé API {provider}, je répondrais en me basant " +
                   "sur le contexte du module.";
        }

        var systemPrompt = $"""
            Tu es un formateur IA expert et pédagogue. Tu réponds aux questions des apprenants
            sur le module suivant : {moduleContext}

            Règles :
            - Réponds en français, de façon claire et concise
            - Adapte ton niveau à l'apprenant
            - Si la question est hors sujet, redirige poliment vers le contenu du module
            - Tes réponses seront lues par un avatar IA, donc évite les formatages markdown complexes
            """;

        return provider switch
        {
            "claude" => await CallAnthropicAsync(userMessage, systemPrompt, history, apiKey!, ct),
            "google" => await CallGoogleAsync(userMessage, systemPrompt, history, apiKey!, ct),
            // openai, groq et « llm » (passerelle générique) partagent l'API OpenAI Chat Completions.
            "openai" or "groq" or "llm" => await CallOpenAiCompatibleAsync(provider, userMessage, systemPrompt, history, apiKey!, ct),
            _ => throw new InvalidOperationException($"Fournisseur LLM inconnu : {provider}")
        };
    }

    // Lit une clé de config en traitant la chaîne vide (env var non renseignée) comme absente.
    private string? Cfg(string key) => string.IsNullOrWhiteSpace(_config[key]) ? null : _config[key];

    // ── Anthropic (Claude) : POST /v1/messages ────────────────────────────────
    private async Task<string> CallAnthropicAsync(string userMessage, string system, IEnumerable<ChatMessage> history, string apiKey, CancellationToken ct)
    {
        var baseUrl = Cfg("LLM:BaseUrl") ?? "https://api.anthropic.com";
        var model = Cfg("LLM:Model") ?? "claude-sonnet-4-6";

        var messages = new List<object>();
        foreach (var msg in history.TakeLast(10)) messages.Add(new { role = msg.Role, content = msg.Content });
        messages.Add(new { role = "user", content = userMessage });

        var body = new { model, max_tokens = 1024, system, messages };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/messages");
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return result.GetProperty("content")[0].GetProperty("text").GetString() ?? "Je n'ai pas pu générer une réponse.";
    }

    // ── OpenAI / Groq / passerelle générique : POST /v1/chat/completions ───────
    private async Task<string> CallOpenAiCompatibleAsync(string provider, string userMessage, string system, IEnumerable<ChatMessage> history, string apiKey, CancellationToken ct)
    {
        var baseUrl = Cfg("LLM:BaseUrl") ?? provider switch
        {
            "groq" => "https://api.groq.com/openai",
            "openai" => "https://api.openai.com",
            _ => throw new InvalidOperationException("LLM:BaseUrl requis pour le fournisseur « llm ».")
        };
        var model = Cfg("LLM:Model") ?? provider switch
        {
            "groq" => "llama-3.3-70b-versatile",
            _ => "gpt-4o-mini"
        };

        var messages = new List<object> { new { role = "system", content = system } };
        foreach (var msg in history.TakeLast(10)) messages.Add(new { role = msg.Role, content = msg.Content });
        messages.Add(new { role = "user", content = userMessage });

        var body = new { model, max_tokens = 1024, messages };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/chat/completions");
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "Je n'ai pas pu générer une réponse.";
    }

    // ── Google Gemini : POST /v1beta/models/{model}:generateContent ───────────
    private async Task<string> CallGoogleAsync(string userMessage, string system, IEnumerable<ChatMessage> history, string apiKey, CancellationToken ct)
    {
        var baseUrl = Cfg("LLM:BaseUrl") ?? "https://generativelanguage.googleapis.com";
        var model = Cfg("LLM:Model") ?? "gemini-1.5-flash";

        var contents = new List<object>();
        foreach (var msg in history.TakeLast(10))
            contents.Add(new { role = msg.Role == "assistant" ? "model" : "user", parts = new[] { new { text = msg.Content } } });
        contents.Add(new { role = "user", parts = new[] { new { text = userMessage } } });

        var body = new
        {
            system_instruction = new { parts = new[] { new { text = system } } },
            contents
        };
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1beta/models/{model}:generateContent?key={apiKey}");
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return result.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "Je n'ai pas pu générer une réponse.";
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
