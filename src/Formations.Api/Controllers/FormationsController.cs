using Formations.Application.Common.Interfaces;
using Formations.Application.Common.Models;
using Formations.Application.Formations.DTOs;
using Formations.Domain.Entities;
using Formations.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Formations.Api.Controllers;

[ApiController]
[Route("api/formations")]
[Produces("application/json")]
public class FormationsController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public FormationsController(IApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] int? categorieId = null,
        [FromQuery] NiveauFormation? niveau = null,
        [FromQuery] string? search = null,
        [FromQuery] bool publishedOnly = true,
        CancellationToken ct = default)
    {
        var query = _context.Formations
            .Include(f => f.Categorie)
            .Include(f => f.FormationFormateurs).ThenInclude(ff => ff.Formateur).ThenInclude(fo => fo.User)
            .AsQueryable();

        if (publishedOnly) query = query.Where(f => f.Statut == StatutFormation.Publie);
        if (categorieId.HasValue) query = query.Where(f => f.CategorieId == categorieId.Value);
        if (niveau.HasValue) query = query.Where(f => f.Niveau == niveau.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(f => f.Titre.Contains(search) || (f.Description != null && f.Description.Contains(search)));

        var totalCount = await query.CountAsync(ct);
        var formations = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FormationSummaryDto(
                f.Id, f.Titre, f.ImageUrl,
                f.Categorie.Nom,
                f.Niveau.ToString(),
                f.DureeHeures, f.Prix,
                f.Inscriptions.Count,
                f.Statut))
            .ToListAsync(ct);

        return Ok(PagedResult<FormationSummaryDto>.Create(formations, totalCount, page, pageSize));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var f = await _context.Formations
            .Include(f => f.Categorie)
            .Include(f => f.Modules)
            .Include(f => f.Inscriptions)
            .Include(f => f.FormationFormateurs).ThenInclude(ff => ff.Formateur).ThenInclude(fo => fo.User)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

        if (f is null) return NotFound();

        return Ok(new FormationDto(
            f.Id, f.Titre, f.Description,
            f.CategorieId, f.Categorie.Nom,
            f.Niveau, f.Niveau.ToString(),
            f.DureeHeures, f.Statut, f.ImageUrl,
            f.Objectifs, f.Prerequis, f.Prix,
            f.Modules.Count, f.Inscriptions.Count,
            f.FormationFormateurs.Select(ff => new FormateurSummaryDto(
                ff.Formateur.Id, ff.Formateur.User.FullName, ff.Formateur.PhotoUrl)).ToList(),
            f.CreatedAt));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Formateur")]
    public async Task<IActionResult> Create([FromBody] CreateFormationRequest request, CancellationToken ct)
    {
        var formation = new Formation
        {
            Titre = request.Titre,
            Description = request.Description,
            CategorieId = request.CategorieId,
            Niveau = request.Niveau,
            DureeHeures = request.DureeHeures,
            ImageUrl = request.ImageUrl,
            Objectifs = request.Objectifs,
            Prerequis = request.Prerequis,
            Prix = request.Prix,
            Statut = StatutFormation.Brouillon
        };

        _context.Formations.Add(formation);
        await _context.SaveChangesAsync(ct);

        if (request.FormateurIds?.Any() == true)
        {
            foreach (var fid in request.FormateurIds)
                _context.FormationFormateurs.Add(new FormationFormateur { FormationId = formation.Id, FormateurId = fid });
            await _context.SaveChangesAsync(ct);
        }

        return CreatedAtAction(nameof(GetById), new { id = formation.Id }, new { id = formation.Id });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Formateur")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFormationRequest request, CancellationToken ct)
    {
        var formation = await _context.Formations
            .Include(f => f.FormationFormateurs)
            .FirstOrDefaultAsync(f => f.Id == id, ct);
        if (formation is null) return NotFound();

        formation.Titre = request.Titre;
        formation.Description = request.Description;
        formation.CategorieId = request.CategorieId;
        formation.Niveau = request.Niveau;
        formation.DureeHeures = request.DureeHeures;
        formation.Statut = request.Statut;
        formation.ImageUrl = request.ImageUrl;
        formation.Objectifs = request.Objectifs;
        formation.Prerequis = request.Prerequis;
        formation.Prix = request.Prix;
        formation.UpdatedAt = DateTime.UtcNow;

        if (request.FormateurIds is not null)
        {
            foreach (var ff in formation.FormationFormateurs.ToList())
                _context.FormationFormateurs.Remove(ff);
            foreach (var fid in request.FormateurIds)
                _context.FormationFormateurs.Add(new FormationFormateur { FormationId = id, FormateurId = fid });
        }

        await _context.SaveChangesAsync(ct);
        return Ok(new { id = formation.Id });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var formation = await _context.Formations.FindAsync([id], ct);
        if (formation is null) return NotFound();
        _context.Formations.Remove(formation);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPut("{id:int}/publish")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Publish(int id, CancellationToken ct)
    {
        var formation = await _context.Formations.FindAsync([id], ct);
        if (formation is null) return NotFound();
        formation.Statut = StatutFormation.Publie;
        formation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        return Ok(new { message = "Formation publiée." });
    }
}
