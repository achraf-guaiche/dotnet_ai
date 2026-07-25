using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Formations.Infrastructure.Persistence;

// Utilisé uniquement par les outils EF (dotnet ef migrations …). Évite l'AutoDetect de la version
// serveur (qui exige une base MySQL joignable) : version figée = génération de migrations hors-ligne.
// Génération : dotnet ef migrations add <Nom> -p src/Formations.Infrastructure -s src/Formations.Infrastructure
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "server=localhost;database=formations_db;user=root;password=root";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0)),
                mysql => mysql.MigrationsAssembly("Formations.Infrastructure"))
            .Options;

        return new ApplicationDbContext(options);
    }
}
