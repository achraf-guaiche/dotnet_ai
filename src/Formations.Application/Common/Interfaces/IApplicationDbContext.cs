using Formations.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Formations.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Categorie> Categories { get; }
    DbSet<Formateur> Formateurs { get; }
    DbSet<Formation> Formations { get; }
    DbSet<FormationFormateur> FormationFormateurs { get; }
    DbSet<Module> Modules { get; }
    DbSet<Cours> Cours { get; }
    DbSet<Exercice> Exercices { get; }
    DbSet<Examen> Examens { get; }
    DbSet<QuestionQuiz> QuestionsQuiz { get; }
    DbSet<Inscription> Inscriptions { get; }
    DbSet<ResultatExamen> ResultatsExamens { get; }
    DbSet<SessionAvatarIA> SessionsAvatarIA { get; }
    DbSet<ActivityLog> ActivityLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
