using Formations.Application.Auth.Services;
using Formations.Application.Common.Interfaces;
using Formations.Infrastructure.Persistence;
using Formations.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Formations.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Default")
            ?? config["ConnectionStrings__Default"]
            ?? "server=localhost;database=formations_db;user=formations_user;password=UserPasswordFort!";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
                mysql =>
                {
                    mysql.EnableRetryOnFailure(3);
                    mysql.CommandTimeout(30);
                    mysql.MigrationsAssembly("Formations.Infrastructure");
                }));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ICertificateService, CertificateService>();
        services.AddScoped<AuthService>();

        services.AddHttpClient<IAvatarIAService, AvatarIAService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
