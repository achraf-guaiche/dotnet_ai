namespace Formations.Application.Common.Interfaces;

public interface ICertificateService
{
    byte[] GenerateCertificate(string apprenantNom, string formationTitre, string numeroCertificat, DateTime dateObtention);
}
