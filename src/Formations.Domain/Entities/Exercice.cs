using Formations.Domain.Common;
using Formations.Domain.Enums;

namespace Formations.Domain.Entities;

public class Exercice : BaseEntity
{
    public int ModuleId { get; set; }
    public Module Module { get; set; } = null!;

    public string Enonce { get; set; } = string.Empty;
    public TypeReponse TypeReponse { get; set; } = TypeReponse.TexteLibre;
    public bool CorrectionAuto { get; set; }
    public string? ReponseCorrecte { get; set; }
    public string? Instructions { get; set; }
    public int Points { get; set; } = 10;
}
