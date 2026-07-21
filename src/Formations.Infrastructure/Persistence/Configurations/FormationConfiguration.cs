using Formations.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Formations.Infrastructure.Persistence.Configurations;

public class FormationConfiguration : IEntityTypeConfiguration<Formation>
{
    public void Configure(EntityTypeBuilder<Formation> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Titre).IsRequired().HasMaxLength(200);
        builder.Property(f => f.Description).HasMaxLength(5000);
        builder.Property(f => f.ImageUrl).HasMaxLength(500);
        builder.Property(f => f.Objectifs).HasMaxLength(2000);
        builder.Property(f => f.Prerequis).HasMaxLength(2000);
        builder.Property(f => f.Prix).HasPrecision(10, 2);
        builder.Property(f => f.Niveau).HasConversion<string>();
        builder.Property(f => f.Statut).HasConversion<string>();

        builder.HasOne(f => f.Categorie)
            .WithMany(c => c.Formations)
            .HasForeignKey(f => f.CategorieId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class FormationFormateurConfiguration : IEntityTypeConfiguration<FormationFormateur>
{
    public void Configure(EntityTypeBuilder<FormationFormateur> builder)
    {
        builder.HasKey(ff => new { ff.FormationId, ff.FormateurId });

        builder.HasOne(ff => ff.Formation)
            .WithMany(f => f.FormationFormateurs)
            .HasForeignKey(ff => ff.FormationId);

        builder.HasOne(ff => ff.Formateur)
            .WithMany(f => f.FormationFormateurs)
            .HasForeignKey(ff => ff.FormateurId);
    }
}

public class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Titre).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Description).HasMaxLength(2000);

        builder.HasOne(m => m.Formation)
            .WithMany(f => f.Modules)
            .HasForeignKey(m => m.FormationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Cours)
            .WithOne(c => c.Module)
            .HasForeignKey<Cours>(c => c.ModuleId);
    }
}

public class CoursConfiguration : IEntityTypeConfiguration<Cours>
{
    public void Configure(EntityTypeBuilder<Cours> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.VideoUrl).HasMaxLength(500);
        builder.Property(c => c.PdfUrl).HasMaxLength(500);
        builder.Property(c => c.Contenu).HasColumnType("longtext");
        builder.Property(c => c.TextePourAvatar).HasColumnType("text");
    }
}

public class ExerciceConfiguration : IEntityTypeConfiguration<Exercice>
{
    public void Configure(EntityTypeBuilder<Exercice> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Enonce).IsRequired().HasColumnType("text");
        builder.Property(e => e.TypeReponse).HasConversion<string>();
        builder.Property(e => e.ReponseCorrecte).HasColumnType("text");

        builder.HasOne(e => e.Module)
            .WithMany(m => m.Exercices)
            .HasForeignKey(e => e.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ExamenConfiguration : IEntityTypeConfiguration<Examen>
{
    public void Configure(EntityTypeBuilder<Examen> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Titre).IsRequired().HasMaxLength(200);
        builder.Property(e => e.NoteMinPassage).HasPrecision(5, 2);

        builder.HasOne(e => e.Module)
            .WithMany(m => m.Examens)
            .HasForeignKey(e => e.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class QuestionQuizConfiguration : IEntityTypeConfiguration<QuestionQuiz>
{
    public void Configure(EntityTypeBuilder<QuestionQuiz> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Enonce).IsRequired().HasColumnType("text");
        builder.Property(q => q.Choix).HasColumnType("json");
        builder.Property(q => q.BonneReponse).IsRequired().HasMaxLength(500);

        builder.HasOne(q => q.Examen)
            .WithMany(e => e.Questions)
            .HasForeignKey(q => q.ExamenId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class InscriptionConfiguration : IEntityTypeConfiguration<Inscription>
{
    public void Configure(EntityTypeBuilder<Inscription> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.ProgressionPourcent).HasPrecision(5, 2);
        builder.Property(i => i.Statut).HasConversion<string>();
        builder.HasIndex(i => new { i.UserId, i.FormationId }).IsUnique();

        builder.HasOne(i => i.User)
            .WithMany(u => u.Inscriptions)
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Formation)
            .WithMany(f => f.Inscriptions)
            .HasForeignKey(i => i.FormationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ResultatExamenConfiguration : IEntityTypeConfiguration<ResultatExamen>
{
    public void Configure(EntityTypeBuilder<ResultatExamen> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Score).HasPrecision(5, 2);
        builder.Property(r => r.ReponsesJson).HasColumnType("json");

        builder.HasOne(r => r.User)
            .WithMany(u => u.ResultatsExamens)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Examen)
            .WithMany(e => e.Resultats)
            .HasForeignKey(r => r.ExamenId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SessionAvatarIAConfiguration : IEntityTypeConfiguration<SessionAvatarIA>
{
    public void Configure(EntityTypeBuilder<SessionAvatarIA> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Historique).HasColumnType("json");
        builder.Property(s => s.SessionExterneId).HasMaxLength(200);

        builder.HasOne(s => s.User)
            .WithMany(u => u.SessionsAvatarIA)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Module)
            .WithMany(m => m.SessionsAvatarIA)
            .HasForeignKey(s => s.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class FormateurConfiguration : IEntityTypeConfiguration<Formateur>
{
    public void Configure(EntityTypeBuilder<Formateur> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Bio).HasColumnType("text");
        builder.Property(f => f.Specialites).HasMaxLength(500);
        builder.Property(f => f.PhotoUrl).HasMaxLength(500);

        builder.HasOne(f => f.User)
            .WithOne(u => u.Formateur)
            .HasForeignKey<Formateur>(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CategorieConfiguration : IEntityTypeConfiguration<Categorie>
{
    public void Configure(EntityTypeBuilder<Categorie> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Nom).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Description).HasMaxLength(500);
        builder.Property(c => c.Icone).HasMaxLength(100);
        builder.HasIndex(c => c.Nom).IsUnique();
    }
}

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Details).HasMaxLength(1000);
        builder.Property(a => a.IpAddress).HasMaxLength(45);
        builder.Property(a => a.UserAgent).HasMaxLength(500);

        builder.HasOne(a => a.User)
            .WithMany(u => u.ActivityLogs)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
