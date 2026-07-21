namespace Formations.Web.Services;

public class FormationWebService
{
    private readonly IApiClient _api;

    public FormationWebService(IApiClient api) => _api = api;

    public Task<FormationsPage?> GetFormationsAsync(int page = 1, int pageSize = 12, int? categorieId = null, string? search = null)
        => _api.GetAsync<FormationsPage>($"api/formations?page={page}&pageSize={pageSize}"
            + (categorieId.HasValue ? $"&categorieId={categorieId}" : "")
            + (!string.IsNullOrEmpty(search) ? $"&search={Uri.EscapeDataString(search)}" : ""));

    public Task<FormationDetail?> GetFormationAsync(int id)
        => _api.GetAsync<FormationDetail>($"api/formations/{id}");

    public Task<List<CategorieItem>?> GetCategoriesAsync()
        => _api.GetAsync<List<CategorieItem>>("api/categories");

    public Task<List<InscriptionItem>?> GetMesFormationsAsync()
        => _api.GetAsync<List<InscriptionItem>>("api/inscriptions/mes-formations");

    public async Task<bool> InscrireAsync(int formationId)
        => await _api.PostAsync<object, object>("api/inscriptions", new { FormationId = formationId }) is not null;

    public Task<ModuleContent?> GetModuleAsync(int moduleId)
        => _api.GetAsync<ModuleContent>($"api/modules/{moduleId}");

    public Task<byte[]?> GetCertificateAsync(int inscriptionId)
        => _api.GetBytesAsync($"api/inscriptions/{inscriptionId}/certificate");
}

public record FormationsPage(IEnumerable<FormationSummary> Items, int TotalCount, int Page, int PageSize, int TotalPages);
public record FormationSummary(int Id, string Titre, string? ImageUrl, string CategorieNom, string NiveauLabel, int DureeHeures, decimal Prix, int NbInscrits, string Statut);
public record FormationDetail(int Id, string Titre, string? Description, int CategorieId, string CategorieNom, string NiveauLabel, int DureeHeures, string? ImageUrl, string? Objectifs, string? Prerequis, decimal Prix, int NbModules, int NbInscrits, List<ModuleSummary> Modules);
public record ModuleSummary(int Id, string Titre, int Ordre, bool IsPublie, bool HasCours);
public record CategorieItem(int Id, string Nom, string? Description, string? Icone, int NbFormations);
public record InscriptionItem(int Id, int FormationId, string FormationTitre, string? FormationImage, string Statut, decimal ProgressionPourcent, int ModulesCompletes, int TotalModules, string? NumeroCertificat);
public record ModuleContent(ModuleInfo Module, CoursInfo? Cours, List<ExerciceInfo> Exercices, List<ExamenInfo> Examens);
public record ModuleInfo(int Id, int FormationId, string Titre, int Ordre, string? Description, bool HasCours);
public record CoursInfo(int Id, string? Contenu, string? VideoUrl, string? PdfUrl, string? TextePourAvatar);
public record ExerciceInfo(int Id, string Enonce, string TypeReponse, bool CorrectionAuto, string? Instructions);
public record ExamenInfo(int Id, string Titre, int DureeMinutes, decimal NoteMinPassage, int TentativesMax, int NbQuestions);
