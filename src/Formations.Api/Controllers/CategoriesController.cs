using Formations.Application.Categories.DTOs;
using Formations.Application.Common.Interfaces;
using Formations.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Formations.Api.Controllers;

[ApiController]
[Route("api/categories")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public CategoriesController(IApplicationDbContext context) => _context = context;

    [HttpGet]
    [ProducesResponseType(typeof(List<CategorieDto>), 200)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var categories = await _context.Categories
            .OrderBy(c => c.Nom)
            .Select(c => new CategorieDto(c.Id, c.Nom, c.Description, c.Icone,
                c.Formations.Count(f => f.Statut == Domain.Enums.StatutFormation.Publie)))
            .ToListAsync(ct);

        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CategorieDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var c = await _context.Categories.Include(x => x.Formations).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return NotFound();
        return Ok(new CategorieDto(c.Id, c.Nom, c.Description, c.Icone,
            c.Formations.Count(f => f.Statut == Domain.Enums.StatutFormation.Publie)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CategorieDto), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateCategorieRequest request, CancellationToken ct)
    {
        if (await _context.Categories.AnyAsync(c => c.Nom == request.Nom, ct))
            return BadRequest(new { error = "Une catégorie avec ce nom existe déjà." });

        var categorie = new Categorie { Nom = request.Nom, Description = request.Description, Icone = request.Icone };
        _context.Categories.Add(categorie);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = categorie.Id },
            new CategorieDto(categorie.Id, categorie.Nom, categorie.Description, categorie.Icone, 0));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(CategorieDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategorieRequest request, CancellationToken ct)
    {
        var categorie = await _context.Categories.FindAsync([id], ct);
        if (categorie is null) return NotFound();

        categorie.Nom = request.Nom;
        categorie.Description = request.Description;
        categorie.Icone = request.Icone;
        categorie.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        return Ok(new CategorieDto(categorie.Id, categorie.Nom, categorie.Description, categorie.Icone, 0));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var categorie = await _context.Categories.Include(c => c.Formations).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (categorie is null) return NotFound();
        if (categorie.Formations.Any()) return Conflict(new { error = "Impossible de supprimer : des formations utilisent cette catégorie." });

        _context.Categories.Remove(categorie);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }
}
