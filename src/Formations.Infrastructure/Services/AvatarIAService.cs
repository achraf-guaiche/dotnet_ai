using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Formations.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Formations.Infrastructure.Services;

public class AvatarIAService : IAvatarIAService, ICourseGenerationService
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
        var personaId = Cfg("AvatarIA:PersonaId")
            ?? throw new InvalidOperationException("AvatarIA:PersonaId est requis pour Anam (legacy token).");

        // Format attendu par /auth/session-token pour un token "legacy" : { personaId }
        // (pas personaConfig.id — le SDK valide "personaId" IN options à la construction)
        var request = new { personaId };

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

        return await CallProviderAsync(provider, userMessage, systemPrompt, history, apiKey!, ct);
    }

    // Dispatch unique vers le fournisseur LLM configuré (réutilisé par le chat ET la génération).
    private async Task<string> CallProviderAsync(string provider, string userMessage, string systemPrompt,
        IEnumerable<ChatMessage> history, string apiKey, CancellationToken ct) => provider switch
    {
        "claude" => await CallAnthropicAsync(userMessage, systemPrompt, history, apiKey, ct),
        "google" => await CallGoogleAsync(userMessage, systemPrompt, history, apiKey, ct),
        // openai, groq et « llm » (passerelle générique) partagent l'API OpenAI Chat Completions.
        "openai" or "groq" or "llm" => await CallOpenAiCompatibleAsync(provider, userMessage, systemPrompt, history, apiKey, ct),
        _ => throw new InvalidOperationException($"Fournisseur LLM inconnu : {provider}")
    };

    // ── Génération de contenu de module (ICourseGenerationService) ─────────────
    public async Task<GeneratedCourse> GenerateModuleContentAsync(
        string prompt, string moduleTitre, string formationTitre, CancellationToken ct = default)
    {
        var provider = (_config["LLM:Provider"] ?? "claude").ToLowerInvariant();
        var apiKey = _config["LLM:ApiKey"];

        // Mode dev / mock : pas de clé → contenu d'exemple déterministe (aucun appel externe).
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogInformation("[MOCK LLM:{Provider}] Génération de module simulée pour : {Titre}", provider, moduleTitre);
            return MockGeneratedCourse(prompt, moduleTitre);
        }

        var system = """
            Tu es un concepteur pédagogique. À partir de la demande du formateur, tu produis le
            contenu d'un module de formation. Tu réponds UNIQUEMENT avec un objet JSON valide, sans
            texte autour, sans bloc de code markdown, au format exact suivant :
            {
              "contenu": "<le cours en HTML simple : titres <h2>/<h3>, paragraphes <p>, listes <ul><li>>",
              "textePourAvatar": "<un script parlé, fluide et sans markdown, que le formateur IA vidéo récitera pour présenter le module (150-300 mots)>",
              "dureeEstimeeMinutes": <entier>,
              "quiz": [
                {"enonce": "<question>", "choix": ["A","B","C","D"], "bonneReponse": "<le texte exact de la bonne option>", "explication": "<courte explication>", "points": 1}
              ],
              "exercices": [
                {"enonce": "<consigne de l'exercice>", "typeReponse": "TexteLibre|ChoixMultiple|VraiOuFaux", "reponseCorrecte": "<réponse attendue, ou null si ouvert>", "points": 10}
              ]
            }
            Génère entre 3 et 5 questions de quiz et entre 2 et 4 exercices. Réponds en français.
            """;
        var user = $"Formation : « {formationTitre} ». Module : « {moduleTitre} ». Demande du formateur : {prompt}";

        var raw = await CallProviderAsync(provider, user, system, Array.Empty<ChatMessage>(), apiKey!, ct);
        return ParseGeneratedCourse(raw, moduleTitre);
    }

    // Extrait et désérialise le JSON renvoyé par le LLM (tolère les blocs ```json … ```).
    private GeneratedCourse ParseGeneratedCourse(string raw, string moduleTitre)
    {
        try
        {
            var json = raw.Trim();
            var start = json.IndexOf('{');
            var end = json.LastIndexOf('}');
            if (start >= 0 && end > start) json = json.Substring(start, end - start + 1);

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = JsonSerializer.Deserialize<GeneratedCourseJson>(json, opts)
                      ?? throw new InvalidOperationException("JSON vide.");

            var quiz = (dto.Quiz ?? new()).Select(q => new GeneratedQuestion(
                q.Enonce ?? "", q.Choix ?? new(), q.BonneReponse ?? "", q.Explication,
                q.Points <= 0 ? 1 : q.Points)).ToList();

            var exercices = (dto.Exercices ?? new()).Select(e => new GeneratedExercice(
                e.Enonce ?? "", string.IsNullOrWhiteSpace(e.TypeReponse) ? "TexteLibre" : e.TypeReponse,
                string.IsNullOrWhiteSpace(e.ReponseCorrecte) ? null : e.ReponseCorrecte,
                e.Points <= 0 ? 10 : e.Points)).Where(e => !string.IsNullOrWhiteSpace(e.Enonce)).ToList();

            return new GeneratedCourse(
                dto.Contenu ?? "", dto.TextePourAvatar ?? "",
                dto.DureeEstimeeMinutes <= 0 ? 15 : dto.DureeEstimeeMinutes, quiz, exercices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Réponse LLM non parsable pour la génération du module {Titre} — repli mock", moduleTitre);
            return MockGeneratedCourse("(réponse IA non exploitable)", moduleTitre);
        }
    }

    private static GeneratedCourse MockGeneratedCourse(string prompt, string moduleTitre) => new(
        Contenu: $"<h2>{moduleTitre}</h2><p>Contenu généré (mode démo) à partir de la consigne : " +
                 $"<em>{System.Net.WebUtility.HtmlEncode(prompt)}</em>.</p><ul><li>Notion clé 1</li>" +
                 "<li>Notion clé 2</li><li>Notion clé 3</li></ul>",
        TextePourAvatar: $"Bonjour et bienvenue dans le module « {moduleTitre} ». Dans cette présentation, " +
                 "nous allons découvrir les notions essentielles, étape par étape. N'hésitez pas à me poser " +
                 "vos questions à tout moment grâce au chat.",
        DureeEstimeeMinutes: 15,
        Quiz: new()
        {
            new("Quelle est la première notion clé abordée dans ce module ?",
                new() { "Notion clé 1", "Notion clé 2", "Notion clé 3", "Aucune" }, "Notion clé 1",
                "C'est la première notion présentée.", 1),
            new("Combien de notions clés sont présentées ?",
                new() { "1", "2", "3", "4" }, "3", "Le module en présente trois.", 1),
            new("Comment poser une question pendant la présentation ?",
                new() { "Par email", "Via le chat", "En appelant", "Impossible" }, "Via le chat",
                "Le prof IA répond en direct dans le chat.", 1),
        },
        Exercices: new()
        {
            new($"Expliquez avec vos propres mots la première notion clé du module « {moduleTitre} ».",
                "TexteLibre", null, 10),
            new("La notion clé 1 est-elle abordée avant la notion clé 2 ?",
                "VraiOuFaux", "Vrai", 5),
        });

    // Miroir de désérialisation (souple face aux variations de casse/champ du LLM).
    private sealed class GeneratedCourseJson
    {
        public string? Contenu { get; set; }
        public string? TextePourAvatar { get; set; }
        public int DureeEstimeeMinutes { get; set; }
        public List<GeneratedQuestionJson>? Quiz { get; set; }
        public List<GeneratedExerciceJson>? Exercices { get; set; }
    }

    private sealed class GeneratedQuestionJson
    {
        public string? Enonce { get; set; }
        public List<string>? Choix { get; set; }
        public string? BonneReponse { get; set; }
        public string? Explication { get; set; }
        public int Points { get; set; }
    }

    private sealed class GeneratedExerciceJson
    {
        public string? Enonce { get; set; }
        public string? TypeReponse { get; set; }
        public string? ReponseCorrecte { get; set; }
        public int Points { get; set; }
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

        try
        {
            if (provider == "anam")
            {
                var baseUrl = (Cfg("AvatarIA:BaseUrl") ?? "https://api.anam.ai/v1").TrimEnd('/');
                var request = new { session_id = sessionId, text };
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config["AvatarIA:ApiKey"]}");
                var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/sessions/speak", request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Anam TTS a répondu {Status} pour la session {Session} — TTS ignoré", (int)response.StatusCode, sessionId);
                    return "";
                }
                var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
                return result.TryGetProperty("audio_url", out var url) ? url.GetString()! : "";
            }

            if (provider == "heygen")
            {
                var request = new { session_id = sessionId, text, task_type = "repeat" };
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-api-key", _config["AvatarIA:ApiKey"]);
                var response = await _httpClient.PostAsJsonAsync("https://api.heygen.com/v1/streaming.task", request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("HeyGen TTS a répondu {Status} pour la session {Session} — TTS ignoré", (int)response.StatusCode, sessionId);
                    return sessionId;
                }
                return sessionId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur TTS avatar ({Provider}) — dégradation gracieuse, le texte reste disponible", provider);
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
            // La session WebRTC Anam est fermée côté client via client.stopStreaming()
            // qui ferme le WebSocket de signaling — Anam libère le slot automatiquement.
            // Il n'existe pas d'endpoint REST /sessions/{id}/end dans l'API Anam v1.
            _logger.LogInformation("Anam session {SessionId} — fermeture gérée côté SDK (WebSocket close)", sessionId);
        }
        else if (provider == "heygen")
        {
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _config["AvatarIA:ApiKey"]);
            await _httpClient.PostAsJsonAsync("https://api.heygen.com/v1/streaming.stop", new { session_id = sessionId }, ct);
        }
    }
}
