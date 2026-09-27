using Microsoft.EntityFrameworkCore;

namespace Psycheflow.Api.Common.Persistence;

public static class PersistenceSetup
{
    public const string ConnectionStringName = "Postgres";
    public const string MigrationsHistoryTable = "__ef_migrations_history";
    public const string MigrateOnStartupKey = "Database:MigrateOnStartup";

    public static IServiceCollection AddPersistence(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddScoped<AuditingInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            string connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"ConnectionStrings:{ConnectionStringName} não configurada.");

            options
                .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(MigrationsHistoryTable))
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(serviceProvider.GetRequiredService<AuditingInterceptor>());

            // Dados de demonstração só em desenvolvimento; dados essenciais (roles) vêm das migrations.
            if (environment.IsDevelopment())
            {
                options.UseAsyncSeeding(DevData.SeedAsync);
            }
        });

        return services;
    }

    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
}
