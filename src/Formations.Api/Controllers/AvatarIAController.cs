using Formations.Application.AvatarIA.DTOs;
using Formations.Application.Common.Interfaces;
using Formations.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace Formations.Api.Controllers;

[ApiController]
[Route("api/avatar")]
[Authorize]
[Produces("application/json")]
public class AvatarIAController : ControllerBase
{
    private readonly IAvatarIAService _avatarService;
    private readonly IApplicationDbContext _context;

    public AvatarIAController(IAvatarIAService avatarService, IApplicationDbContext context)
    {
        _avatarService = avatarService;
        _context = context;
    }

    [HttpPost("sessions/start")]
    public async Task<IActionResult> StartSession([FromBody] StartSessionRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var module = await _context.Modules
            .Include(m => m.Cours)
            .FirstOrDefaultAsync(m => m.Id == request.ModuleId, ct);
        if (module is null) return NotFound(new { error = "Module introuvable." });

        var texte = request.TextePresentation ?? module.Cours?.TextePourAvatar
            ?? $"Bienvenue dans le module {module.Titre}. {module.Description}";

        var avatarSession = await _avatarService.CreateSessionAsync(texte, ct);

        var dbSession = new SessionAvatarIA
        {
            UserId = userId,
            ModuleId = request.ModuleId,
            SessionExterneId = avatarSession.SessionId,
            Historique = "[]"
        };
        _context.SessionsAvatarIA.Add(dbSession);
        await _context.SaveChangesAsync(ct);

        return Ok(new StartSessionResponse(
            avatarSession.SessionId,
            avatarSession.StreamUrl,
            avatarSession.EmbedToken,
            _avatarService.IsMockMode,
            dbSession.Id));
    }

    [HttpPost("sessions/message")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var dbSession = await _context.SessionsAvatarIA
            .Include(s => s.Module).ThenInclude(m => m.Cours)
            .FirstOrDefaultAsync(s => s.Id == request.DbSessionId && s.UserId == userId, ct);
        if (dbSession is null) return NotFound();

        var history = JsonSerializer.Deserialize<List<Formations.Application.Common.Interfaces.ChatMessage>>(dbSession.Historique)
            ?? [];

        var moduleContext = $"{dbSession.Module.Titre}: {dbSession.Module.Description ?? ""} {dbSession.Module.Cours?.TextePourAvatar ?? ""}";
        var aiResponse = await _avatarService.GetAIResponseAsync(request.Message, moduleContext, history, ct);

        history.Add(new Formations.Application.Common.Interfaces.ChatMessage("user", request.Message, DateTime.UtcNow));
        history.Add(new Formations.Application.Common.Interfaces.ChatMessage("assistant", aiResponse, DateTime.UtcNow));
        dbSession.Historique = JsonSerializer.Serialize(history);
        await _context.SaveChangesAsync(ct);

        string? audioUrl = null;
        if (!_avatarService.IsMockMode)
            audioUrl = await _avatarService.TextToAvatarSpeechAsync(aiResponse, request.SessionId, ct);

        return Ok(new SendMessageResponse(aiResponse, audioUrl, null));
    }

    [HttpPost("sessions/{dbSessionId:int}/end")]
    public async Task<IActionResult> EndSession(int dbSessionId, [FromBody] EndSessionRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var dbSession = await _context.SessionsAvatarIA
            .FirstOrDefaultAsync(s => s.Id == dbSessionId && s.UserId == userId, ct);
        if (dbSession is null) return NotFound();

        dbSession.DateFin = DateTime.UtcNow;
        dbSession.DureeSecondes = request.DureeSecondes;
        await _context.SaveChangesAsync(ct);

        if (dbSession.SessionExterneId is not null)
            await _avatarService.EndSessionAsync(dbSession.SessionExterneId, ct);

        return Ok(new { message = "Session terminée." });
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Matérialisation d'abord : JsonSerializer.Deserialize (argument optionnel)
        // ne peut pas figurer dans un arbre d'expression EF. Projection en mémoire ensuite.
        var entities = await _context.SessionsAvatarIA
            .Include(s => s.Module)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.DateSession)
            .ToListAsync(ct);

        var sessions = entities.Select(s => new SessionHistoriqueDto(
                s.Id, s.ModuleId, s.Module.Titre, s.DateSession, s.DateFin, s.DureeSecondes,
                JsonSerializer.Deserialize<List<ChatMessageDto>>(s.Historique) ?? new()))
            .ToList();

        return Ok(sessions);
    }
}

public record EndSessionRequest(int DureeSecondes);
