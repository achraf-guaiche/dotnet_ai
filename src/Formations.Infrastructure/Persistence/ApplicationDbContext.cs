using Formations.Application.Common.Interfaces;
using Formations.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Formations.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Categorie> Categories => Set<Categorie>();
    public DbSet<Formateur> Formateurs => Set<Formateur>();
    public DbSet<Formation> Formations => Set<Formation>();
    public DbSet<FormationFormateur> FormationFormateurs => Set<FormationFormateur>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<Cours> Cours => Set<Cours>();
    public DbSet<Exercice> Exercices => Set<Exercice>();
    public DbSet<Examen> Examens => Set<Examen>();
    public DbSet<QuestionQuiz> QuestionsQuiz => Set<QuestionQuiz>();
    public DbSet<Inscription> Inscriptions => Set<Inscription>();
    public DbSet<ResultatExamen> ResultatsExamens => Set<ResultatExamen>();
    public DbSet<SessionAvatarIA> SessionsAvatarIA => Set<SessionAvatarIA>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is Domain.Common.BaseEntity && e.State == EntityState.Modified);

        foreach (var entry in entries)
            ((Domain.Common.BaseEntity)entry.Entity).UpdatedAt = DateTime.UtcNow;

        return base.SaveChangesAsync(cancellationToken);
    }
}
