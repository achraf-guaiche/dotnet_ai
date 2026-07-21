using Formations.Application.Common.Interfaces;
using Formations.Application.Formateurs.DTOs;
using Formations.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Formations.Api.Controllers;

[ApiController]
[Route("api/formateurs")]
[Produces("application/json")]
public class FormateursController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public FormateursController(IApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var formateurs = await _context.Formateurs
            .Include(f => f.User)
            .Include(f => f.FormationFormateurs)
            .Where(f => f.IsActive)
            .Select(f => new FormateurDto(
                f.Id, f.UserId, f.User.FullName, f.User.Email!,
                f.Bio, f.Specialites, f.PhotoUrl, f.IsActive,
                f.FormationFormateurs.Count))
            .ToListAsync(ct);

        return Ok(formateurs);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var f = await _context.Formateurs
            .Include(f => f.User)
            .Include(f => f.FormationFormateurs).ThenInclude(ff => ff.Formation)
            .FirstOrDefaultAsync(f => f.Id == id, ct);
        if (f is null) return NotFound();

        return Ok(new
        {
            formateur = new FormateurDto(f.Id, f.UserId, f.User.FullName, f.User.Email!, f.Bio, f.Specialites, f.PhotoUrl, f.IsActive, f.FormationFormateurs.Count),
            formations = f.FormationFormateurs.Select(ff => new { ff.Formation.Id, ff.Formation.Titre, ff.IsLead })
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateFormateurRequest request, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null) return BadRequest(new { error = "Utilisateur introuvable." });

        if (await _context.Formateurs.AnyAsync(f => f.UserId == request.UserId, ct))
            return Conflict(new { error = "Un profil formateur existe déjà pour cet utilisateur." });

        var formateur = new Formateur
        {
            UserId = request.UserId,
            Bio = request.Bio,
            Specialites = request.Specialites,
            PhotoUrl = request.PhotoUrl
        };
        _context.Formateurs.Add(formateur);

        await _userManager.RemoveFromRoleAsync(user, "Apprenant");
        await _userManager.AddToRoleAsync(user, "Formateur");

        await _context.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = formateur.Id }, new { id = formateur.Id });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Formateur")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFormateurRequest request, CancellationToken ct)
    {
        var formateur = await _context.Formateurs.FindAsync([id], ct);
        if (formateur is null) return NotFound();

        formateur.Bio = request.Bio;
        formateur.Specialites = request.Specialites;
        formateur.PhotoUrl = request.PhotoUrl;
        formateur.IsActive = request.IsActive;
        formateur.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return Ok(new { id = formateur.Id });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var formateur = await _context.Formateurs.FindAsync([id], ct);
        if (formateur is null) return NotFound();
        formateur.IsActive = false;
        await _context.SaveChangesAsync(ct);
        return Ok(new { message = "Formateur désactivé." });
    }
}
