using Formations.Application.Common.Interfaces;
using Formations.Application.Modules.DTOs;
using Formations.Domain.Entities;
using Formations.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Formations.Api.Controllers;

[ApiController]
[Route("api/modules")]
[Produces("application/json")]
public class ModulesController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public ModulesController(IApplicationDbContext context) => _context = context;

    [HttpGet("formation/{formationId:int}")]
    public async Task<IActionResult> GetByFormation(int formationId, CancellationToken ct)
    {
        var modules = await _context.Modules
            .Include(m => m.Cours)
            .Include(m => m.Exercices)
            .Include(m => m.Examens)
            .Where(m => m.FormationId == formationId)
            .OrderBy(m => m.Ordre)
            .Select(m => new ModuleDto(
                m.Id, m.FormationId, m.Formation.Titre,
                m.Titre, m.Ordre, m.Description, m.IsPublie,
                m.Cours != null, m.Exercices.Count, m.Examens.Count))
            .ToListAsync(ct);

        return Ok(modules);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var m = await _context.Modules
            .Include(m => m.Formation)
            .Include(m => m.Cours)
            .Include(m => m.Exercices)
            .Include(m => m.Examens).ThenInclude(e => e.Questions)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

        if (m is null) return NotFound();

        return Ok(new
        {
            module = new ModuleDto(m.Id, m.FormationId, m.Formation.Titre, m.Titre, m.Ordre, m.Description, m.IsPublie, m.Cours != null, m.Exercices.Count, m.Examens.Count),
            cours = m.Cours is null ? null : new CoursDto(m.Cours.Id, m.Cours.ModuleId, m.Cours.Contenu, m.Cours.VideoUrl, m.Cours.PdfUrl, m.Cours.TextePourAvatar, m.Cours.DureeEstimeeMinutes),
            exercices = m.Exercices.Select(e => new ExerciceDto(e.Id, e.ModuleId, e.Enonce, e.TypeReponse.ToString(), e.CorrectionAuto, e.Instructions, e.Points)),
            examens = m.Examens.Select(e => new ExamenDto(e.Id, e.ModuleId, e.Titre, e.Description, e.DureeMinutes, e.NoteMinPassage, e.TentativesMax, e.Questions.Count, e.IsActif))
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Formateur")]
    public async Task<IActionResult> Create([FromBody] CreateModuleRequest request, CancellationToken ct)
    {
        var module = new Module
        {
            FormationId = request.FormationId,
            Titre = request.Titre,
            Ordre = request.Ordre,
            Description = request.Description
        };
        _context.Modules.Add(module);
        await _context.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = module.Id }, new { id = module.Id });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Formateur")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateModuleRequest request, CancellationToken ct)
    {
        var module = await _context.Modules.FindAsync([id], ct);
        if (module is null) return NotFound();

        module.Titre = request.Titre;
        module.Ordre = request.Ordre;
        module.Description = request.Description;
        module.IsPublie = request.IsPublie;
        module.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        return Ok(new { id = module.Id });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var module = await _context.Modules.FindAsync([id], ct);
        if (module is null) return NotFound();
        _context.Modules.Remove(module);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{moduleId:int}/cours")]
    [Authorize(Roles = "Admin,Formateur")]
    public async Task<IActionResult> CreateCours(int moduleId, [FromBody] CreateCoursRequest request, CancellationToken ct)
    {
        if (await _context.Cours.AnyAsync(c => c.ModuleId == moduleId, ct))
            return Conflict(new { error = "Ce module a déjà un cours. Utilisez PUT pour le modifier." });

        var cours = new Cours
        {
            ModuleId = moduleId,
            Contenu = request.Contenu,
            VideoUrl = request.VideoUrl,
            PdfUrl = request.PdfUrl,
            TextePourAvatar = request.TextePourAvatar,
            DureeEstimeeMinutes = request.DureeEstimeeMinutes
        };
        _context.Cours.Add(cours);
        await _context.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = moduleId }, new { id = cours.Id });
    }

    [HttpPut("{moduleId:int}/cours")]
    [Authorize(Roles = "Admin,Formateur")]
    public async Task<IActionResult> UpdateCours(int moduleId, [FromBody] UpdateCoursRequest request, CancellationToken ct)
    {
        var cours = await _context.Cours.FirstOrDefaultAsync(c => c.ModuleId == moduleId, ct);
        if (cours is null) return NotFound();

        cours.Contenu = request.Contenu;
        cours.VideoUrl = request.VideoUrl;
        cours.PdfUrl = request.PdfUrl;
        cours.TextePourAvatar = request.TextePourAvatar;
        cours.DureeEstimeeMinutes = request.DureeEstimeeMinutes;
        cours.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        return Ok(new { id = cours.Id });
    }

    [HttpPost("{moduleId:int}/exercices")]
    [Authorize(Roles = "Admin,Formateur")]
    public async Task<IActionResult> CreateExercice(int moduleId, [FromBody] CreateExerciceRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<TypeReponse>(request.TypeReponse, true, out var typeReponse))
            return BadRequest(new { error = "Type de réponse invalide." });

        var exercice = new Exercice
        {
            ModuleId = moduleId,
            Enonce = request.Enonce,
            TypeReponse = typeReponse,
            CorrectionAuto = request.CorrectionAuto,
            ReponseCorrecte = request.ReponseCorrecte,
            Instructions = request.Instructions,
            Points = request.Points
        };
        _context.Exercices.Add(exercice);
        await _context.SaveChangesAsync(ct);
        return Created($"/api/modules/{moduleId}", new { id = exercice.Id });
    }

    [HttpPost("{moduleId:int}/examens")]
    [Authorize(Roles = "Admin,Formateur")]
    public async Task<IActionResult> CreateExamen(int moduleId, [FromBody] CreateExamenRequest request, CancellationToken ct)
    {
        var examen = new Examen
        {
            ModuleId = moduleId,
            Titre = request.Titre,
            Description = request.Description,
            DureeMinutes = request.DureeMinutes,
            NoteMinPassage = request.NoteMinPassage,
            TentativesMax = request.TentativesMax
        };
        _context.Examens.Add(examen);
        await _context.SaveChangesAsync(ct);
        return Created($"/api/modules/{moduleId}", new { id = examen.Id });
    }

    [HttpPost("examens/{examenId:int}/questions")]
    [Authorize(Roles = "Admin,Formateur")]
    public async Task<IActionResult> AddQuestion(int examenId, [FromBody] CreateQuestionQuizRequest request, CancellationToken ct)
    {
        var question = new QuestionQuiz
        {
            ExamenId = examenId,
            Enonce = request.Enonce,
            Choix = JsonSerializer.Serialize(request.Choix),
            BonneReponse = request.BonneReponse,
            Explication = request.Explication,
            Points = request.Points,
            Ordre = request.Ordre
        };
        _context.QuestionsQuiz.Add(question);
        await _context.SaveChangesAsync(ct);
        return Created($"/api/modules/examens/{examenId}", new { id = question.Id });
    }

    [HttpGet("examens/{examenId:int}/questions")]
    [Authorize]
    public async Task<IActionResult> GetExamenQuestions(int examenId, CancellationToken ct)
    {
        var examen = await _context.Examens
            .Include(e => e.Questions.OrderBy(q => q.Ordre))
            .FirstOrDefaultAsync(e => e.Id == examenId && e.IsActif, ct);
        if (examen is null) return NotFound();

        var questions = examen.Questions.Select(q => new
        {
            q.Id,
            q.Enonce,
            Choix = JsonSerializer.Deserialize<List<string>>(q.Choix) ?? new List<string>(),
            q.Points,
            q.Ordre
        }).ToList();

        return Ok(new
        {
            examen = new ExamenDto(examen.Id, examen.ModuleId, examen.Titre, examen.Description,
                examen.DureeMinutes, examen.NoteMinPassage, examen.TentativesMax, examen.Questions.Count, examen.IsActif),
            questions
        });
    }

    [HttpPost("examens/{examenId:int}/attempt")]
    [Authorize]
    public async Task<IActionResult> AttemptExamen(int examenId, [FromBody] ExamenAttemptRequest request, CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;

        var examen = await _context.Examens
            .Include(e => e.Questions)
            .FirstOrDefaultAsync(e => e.Id == examenId, ct);
        if (examen is null) return NotFound();

        var tentatives = await _context.ResultatsExamens
            .CountAsync(r => r.UserId == userId && r.ExamenId == examenId, ct);

        if (tentatives >= examen.TentativesMax)
            return BadRequest(new { error = $"Nombre maximum de tentatives atteint ({examen.TentativesMax})." });

        var detailsParQuestion = new Dictionary<int, bool>();
        var review = new List<QuestionReviewDto>();
        int correctCount = 0;
        int totalPoints = examen.Questions.Sum(q => q.Points);
        int earnedPoints = 0;

        foreach (var question in examen.Questions)
        {
            var userAnswer = request.Reponses.GetValueOrDefault(question.Id, "");
            var isCorrect = string.Equals(userAnswer.Trim(), question.BonneReponse.Trim(), StringComparison.OrdinalIgnoreCase);
            detailsParQuestion[question.Id] = isCorrect;
            review.Add(new QuestionReviewDto(question.Id, question.Enonce, userAnswer, question.BonneReponse, question.Explication, isCorrect, question.Points));
            if (isCorrect)
            {
                correctCount++;
                earnedPoints += question.Points;
            }
        }

        var score = totalPoints > 0 ? (decimal)earnedPoints / totalPoints * 100 : 0;
        var isReussi = score >= examen.NoteMinPassage;

        var resultat = new ResultatExamen
        {
            UserId = userId,
            ExamenId = examenId,
            Score = Math.Round(score, 2),
            TentativeNumero = tentatives + 1,
            IsReussi = isReussi,
            DureeSecondes = request.DureeSecondes,
            ReponsesJson = JsonSerializer.Serialize(request.Reponses)
        };

        _context.ResultatsExamens.Add(resultat);
        await _context.SaveChangesAsync(ct);

        return Ok(new ExamenAttemptResult(
            Math.Round(score, 2), isReussi, correctCount,
            examen.Questions.Count, tentatives + 1, detailsParQuestion, review));
    }
}
