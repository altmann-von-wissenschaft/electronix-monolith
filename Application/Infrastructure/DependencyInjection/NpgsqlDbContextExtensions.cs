using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.DependencyInjection;

public static class NpgsqlDbContextExtensions
{
    /// <summary>
    /// Registers an EF Core DbContext against PostgreSQL with shared monolith conventions (schema-specific migration history, split queries, retries).
    /// </summary>
    public static void AddElectronixNpgsqlDbContext<TContext>(
        this IServiceCollection services,
        string connectionString,
        string migrationsSchema)
        where TContext : DbContext
    {
        services.AddDbContext<TContext>(options =>
            options.UseNpgsql(connectionString, x =>
            {
                x.MigrationsHistoryTable("__EFMigrationsHistory", migrationsSchema);
                x.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                x.EnableRetryOnFailure();
            }));
    }
}
