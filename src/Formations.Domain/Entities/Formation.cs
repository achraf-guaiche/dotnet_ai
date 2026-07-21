using Formations.Domain.Common;
using Formations.Domain.Enums;

namespace Formations.Domain.Entities;

public class Formation : BaseEntity
{
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategorieId { get; set; }
    public Categorie Categorie { get; set; } = null!;

    public NiveauFormation Niveau { get; set; } = NiveauFormation.Debutant;
    public int DureeHeures { get; set; }
    public StatutFormation Statut { get; set; } = StatutFormation.Brouillon;
    public string? ImageUrl { get; set; }
    public string? Objectifs { get; set; }
    public string? Prerequis { get; set; }
    public decimal Prix { get; set; }

    public ICollection<Module> Modules { get; set; } = [];
    public ICollection<FormationFormateur> FormationFormateurs { get; set; } = [];
    public ICollection<Inscription> Inscriptions { get; set; } = [];
}
