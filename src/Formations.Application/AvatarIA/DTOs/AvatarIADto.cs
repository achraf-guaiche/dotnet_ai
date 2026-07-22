namespace Formations.Application.AvatarIA.DTOs;

public record StartSessionRequest(int ModuleId, string? TextePresentation);

public record StartSessionResponse(
    string SessionId,
    string? StreamUrl,
    string? EmbedToken,
    bool IsMockMode,
    int DbSessionId,
    string InitialMessage,
    string AvatarProvider,   // "mock" | "anam" | "heygen"
    string? AvatarPersonaId  // Anam persona ID — requis par le SDK JS pour createClient
);

public record SendMessageRequest(string SessionId, int DbSessionId, string Message);

public record SendMessageResponse(string AvatarResponse, string? AudioUrl, string? VideoStreamUrl);

public record SessionHistoriqueDto(
    int Id,
    int ModuleId,
    string ModuleTitre,
    DateTime DateSession,
    DateTime? DateFin,
    int DureeSecondes,
    List<ChatMessageDto> Messages
);

public record ChatMessageDto(string Role, string Content, DateTime Timestamp);
