namespace Formations.Application.Common.Interfaces;

public interface IAvatarIAService
{
    Task<AvatarSessionResult> CreateSessionAsync(string textePresentation, CancellationToken ct = default);
    Task<string> GetAIResponseAsync(string userMessage, string moduleContext, IEnumerable<ChatMessage> history, CancellationToken ct = default);
    Task<string> TextToAvatarSpeechAsync(string text, string sessionId, CancellationToken ct = default);
    Task EndSessionAsync(string sessionId, CancellationToken ct = default);
    bool IsMockMode { get; }
}

public record AvatarSessionResult(string SessionId, string StreamUrl, string EmbedToken);

public record ChatMessage(string Role, string Content, DateTime Timestamp);
