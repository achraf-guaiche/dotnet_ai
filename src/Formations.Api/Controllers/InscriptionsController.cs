using Formations.Application.Common.Interfaces;
using Formations.Application.Inscriptions.DTOs;
using Formations.Domain.Entities;
using Formations.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Formations.Api.Controllers;

[ApiController]
[Route("api/inscriptions")]
[Authorize]
[Produces("application/json")]
public class InscriptionsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICertificateService _certificateService;
    private readonly IEmailService _emailService;

    public InscriptionsController(IApplicationDbContext context, ICertificateService certificateService, IEmailService emailService)
    {
        _context = context;
        _certificateService = certificateService;
        _emailService = emailService;
    }

    [HttpGet("mes-formations")]
    public async Task<IActionResult> GetMesFormations(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var inscriptions = await _context.Inscriptions
            .Include(i => i.Formation).ThenInclude(f => f.Categorie)
            .Where(i => i.UserId == userId)
            .OrderByDescending(i => i.DateInscription)
            .Select(i => new InscriptionDto(
                i.Id, i.UserId, i.User.FullName,
                i.FormationId, i.Formation.Titre, i.Formation.ImageUrl,
                i.Statut, i.ProgressionPourcent, i.ModulesCompletes, i.TotalModules,
                i.DateInscription, i.DateTerminee, i.NumeroCertificat))
            .ToListAsync(ct);

        return Ok(inscriptions);
    }

    [HttpPost]
    public async Task<IActionResult> Inscrire([FromBody] CreateInscriptionRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (await _context.Inscriptions.AnyAsync(i => i.UserId == userId && i.FormationId == request.FormationId, ct))
            return Conflict(new { error = "Vous êtes déjà inscrit à cette formation." });

        var formation = await _context.Formations.Include(f => f.Modules)
            .FirstOrDefaultAsync(f => f.Id == request.FormationId && f.Statut == StatutFormation.Publie, ct);
        if (formation is null) return NotFound(new { error = "Formation introuvable ou non publiée." });

        var inscription = new Inscription
        {
            UserId = userId,
            FormationId = request.FormationId,
            Statut = StatutInscription.EnCours,
            TotalModules = formation.Modules.Count
        };

        _context.Inscriptions.Add(inscription);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = inscription.Id }, new { id = inscription.Id });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole("Admin");

        var inscription = await _context.Inscriptions
            .Include(i => i.Formation).Include(i => i.User)
            .FirstOrDefaultAsync(i => i.Id == id && (i.UserId == userId || isAdmin), ct);

        if (inscription is null) return NotFound();

        return Ok(new InscriptionDto(
            inscription.Id, inscription.UserId, inscription.User.FullName,
            inscription.FormationId, inscription.Formation.Titre, inscription.Formation.ImageUrl,
            inscription.Statut, inscription.ProgressionPourcent,
            inscription.ModulesCompletes, inscription.TotalModules,
            inscription.DateInscription, inscription.DateTerminee, inscription.NumeroCertificat));
    }

    [HttpPut("{id:int}/progress")]
    public async Task<IActionResult> UpdateProgress(int id, [FromBody] UpdateProgressRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var inscription = await _context.Inscriptions
            .Include(i => i.Formation).ThenInclude(f => f.Modules)
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId, ct);

        if (inscription is null) return NotFound();

        if (request.IsComplete)
            inscription.ModulesCompletes = Math.Min(inscription.ModulesCompletes + 1, inscription.TotalModules);
        else
            inscription.ModulesCompletes = Math.Max(0, inscription.ModulesCompletes - 1);

        inscription.ProgressionPourcent = inscription.TotalModules > 0
            ? Math.Round((decimal)inscription.ModulesCompletes / inscription.TotalModules * 100, 2)
            : 0;

        if (inscription.ProgressionPourcent >= 100 && inscription.Statut == StatutInscription.EnCours)
        {
            inscription.Statut = StatutInscription.Termine;
            inscription.DateTerminee = DateTime.UtcNow;
            inscription.NumeroCertificat = $"CERT-{DateTime.UtcNow:yyyyMM}-{id:D6}";
        }

        await _context.SaveChangesAsync(ct);
        return Ok(new { progression = inscription.ProgressionPourcent, statut = inscription.Statut.ToString() });
    }

    [HttpGet("{id:int}/certificate")]
    public async Task<IActionResult> GetCertificate(int id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var inscription = await _context.Inscriptions
            .Include(i => i.Formation)
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId, ct);

        if (inscription is null) return NotFound();
        if (inscription.Statut != StatutInscription.Termine && inscription.Statut != StatutInscription.Certifie)
            return BadRequest(new { error = "La formation n'est pas encore terminée." });

        var pdfBytes = _certificateService.GenerateCertificate(
            inscription.User.FullName,
            inscription.Formation.Titre,
            inscription.NumeroCertificat ?? $"CERT-{id:D6}",
            inscription.DateTerminee ?? DateTime.UtcNow);

        return File(pdfBytes, "application/pdf", $"certificat_{inscription.Formation.Titre.Replace(" ", "_")}.pdf");
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var total = await _context.Inscriptions.CountAsync(ct);
        var items = await _context.Inscriptions
            .Include(i => i.User).Include(i => i.Formation)
            .OrderByDescending(i => i.DateInscription)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => new InscriptionDto(
                i.Id, i.UserId, i.User.FullName,
                i.FormationId, i.Formation.Titre, i.Formation.ImageUrl,
                i.Statut, i.ProgressionPourcent, i.ModulesCompletes, i.TotalModules,
                i.DateInscription, i.DateTerminee, i.NumeroCertificat))
            .ToListAsync(ct);

        return Ok(new { items, total, page, pageSize });
    }
}
