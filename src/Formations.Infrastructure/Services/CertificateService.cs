using Formations.Application.Common.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Formations.Infrastructure.Services;

public class CertificateService : ICertificateService
{
    public CertificateService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateCertificate(string apprenantNom, string formationTitre, string numeroCertificat, DateTime dateObtention)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.Background().Background("#F8FAFC");

                page.Content().Column(col =>
                {
                    col.Item().PaddingTop(20).AlignCenter().Text("CERTIFICAT DE RÉUSSITE")
                        .FontSize(32).Bold().FontColor("#1E3A5F");

                    col.Item().PaddingTop(10).AlignCenter().Text("Plateforme Formations")
                        .FontSize(16).FontColor("#4F46E5").Italic();

                    col.Item().PaddingTop(30).LineHorizontal(2).LineColor("#4F46E5");

                    col.Item().PaddingTop(30).AlignCenter().Text("Ce certificat atteste que")
                        .FontSize(14).FontColor("#6B7280");

                    col.Item().PaddingTop(10).AlignCenter().Text(apprenantNom)
                        .FontSize(28).Bold().FontColor("#1E3A5F");

                    col.Item().PaddingTop(15).AlignCenter().Text("a réussi avec succès la formation")
                        .FontSize(14).FontColor("#6B7280");

                    col.Item().PaddingTop(10).AlignCenter().Text(formationTitre)
                        .FontSize(22).SemiBold().FontColor("#4F46E5");

                    col.Item().PaddingTop(30).LineHorizontal(1).LineColor("#E5E7EB");

                    col.Item().PaddingTop(20).Row(row =>
                    {
                        row.RelativeItem().Column(inner =>
                        {
                            inner.Item().AlignCenter().Text("Date d'obtention").FontSize(11).FontColor("#9CA3AF");
                            inner.Item().AlignCenter().Text(dateObtention.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("fr-FR")))
                                .FontSize(14).SemiBold().FontColor("#374151");
                        });

                        row.ConstantItem(2).Background("#E5E7EB");

                        row.RelativeItem().Column(inner =>
                        {
                            inner.Item().AlignCenter().Text("N° Certificat").FontSize(11).FontColor("#9CA3AF");
                            inner.Item().AlignCenter().Text(numeroCertificat)
                                .FontSize(12).FontColor("#374151");
                        });
                    });

                    col.Item().PaddingTop(30).AlignCenter()
                        .Text("Ce certificat a été généré automatiquement par la Plateforme Formations")
                        .FontSize(9).FontColor("#9CA3AF").Italic();
                });
            });
        });

        return document.GeneratePdf();
    }
}
