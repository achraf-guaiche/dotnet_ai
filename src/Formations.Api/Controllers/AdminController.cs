using Formations.Application.Admin.DTOs;
using Formations.Application.Common.Interfaces;
using Formations.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Formations.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(IApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var totalFormations = await _context.Formations.CountAsync(ct);
        var formationsPubliees = await _context.Formations.CountAsync(f => f.Statut == Domain.Enums.StatutFormation.Publie, ct);
        var totalFormateurs = await _context.Formateurs.CountAsync(ct);
        var totalInscriptions = await _context.Inscriptions.CountAsync(ct);
        var totalApprenants = await _userManager.GetUsersInRoleAsync("Apprenant");

        decimal tauxCompletion = totalInscriptions > 0
            ? await _context.Inscriptions.AverageAsync(i => i.ProgressionPourcent, ct)
            : 0;

        var topFormations = await _context.Formations
            .Include(f => f.Inscriptions)
            .Where(f => f.Statut == Domain.Enums.StatutFormation.Publie)
            .OrderByDescending(f => f.Inscriptions.Count)
            .Take(5)
            .Select(f => new FormationStatDto(
                f.Id, f.Titre,
                f.Inscriptions.Count,
                f.Inscriptions.Any() ? f.Inscriptions.Average(i => i.ProgressionPourcent) : 0m))
            .ToListAsync(ct);

        var recentInscriptions = await _context.Inscriptions
            .Include(i => i.User).Include(i => i.Formation)
            .OrderByDescending(i => i.DateInscription)
            .Take(10)
            .Select(i => new InscriptionRecenteDto(i.User.FullName, i.Formation.Titre, i.DateInscription))
            .ToListAsync(ct);

        return Ok(new DashboardStats(
            totalFormations, totalApprenants.Count, totalFormateurs,
            totalInscriptions, Math.Round(tauxCompletion, 1),
            formationsPubliees, topFormations, recentInscriptions));
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var users = _userManager.Users.OrderByDescending(u => u.CreatedAt);
        var total = await users.CountAsync(ct);
        var items = await users.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var result = new List<AdminUserDto>();
        foreach (var user in items)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var nbInscriptions = await _context.Inscriptions.CountAsync(i => i.UserId == user.Id, ct);
            result.Add(new AdminUserDto(user.Id, user.Email!, user.FullName,
                roles.ToList(), user.IsActive, user.CreatedAt, nbInscriptions));
        }

        return Ok(new { items = result, total, page, pageSize });
    }

    [HttpPut("users/{userId}/role")]
    public async Task<IActionResult> ChangeRole(string userId, [FromBody] ChangeUserRoleRequest request, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var allowedRoles = new[] { "Admin", "Formateur", "Apprenant" };
        if (!allowedRoles.Contains(request.Role))
            return BadRequest(new { error = "Rôle invalide. Rôles acceptés : Admin, Formateur, Apprenant." });

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, request.Role);

        // Une promotion en Formateur doit créer la fiche Formateur (cohérent avec l'inscription,
        // US-36) : sans elle, le compte ne peut pas être rattaché à des formations.
        if (request.Role == "Formateur"
            && !await _context.Formateurs.AnyAsync(f => f.UserId == user.Id, ct))
        {
            _context.Formateurs.Add(new Formateur { UserId = user.Id });
            await _context.SaveChangesAsync(ct);
        }

        return Ok(new { message = $"Rôle changé en {request.Role} pour {user.Email}." });
    }

    [HttpPut("users/{userId}/toggle-active")]
    public async Task<IActionResult> ToggleActive(string userId, [FromBody] ToggleUserActiveRequest request, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return Ok(new { message = $"Utilisateur {(request.IsActive ? "activé" : "désactivé")}." });
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetActivityLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? userId = null,
        [FromQuery] string? action = null,
        CancellationToken ct = default)
    {
        var query = _context.ActivityLogs.Include(a => a.User).AsQueryable();
        if (!string.IsNullOrEmpty(userId)) query = query.Where(a => a.UserId == userId);
        if (!string.IsNullOrEmpty(action)) query = query.Where(a => a.Action == action);

        var total = await query.CountAsync(ct);
        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new ActivityLogDto(
                a.Id, a.User != null ? a.User.FullName : null,
                a.Action, a.Details, a.IpAddress, a.CreatedAt))
            .ToListAsync(ct);

        return Ok(new { items = logs, total, page, pageSize });
    }
}
