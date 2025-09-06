using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;

namespace BasicBudget.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for creating BasicBudgetDbContext during migrations and tooling operations
/// </summary>
public class BasicBudgetDbContextFactory : IDesignTimeDbContextFactory<BasicBudgetDbContext>
{
    public BasicBudgetDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BasicBudgetDbContext>();

        // Use a default connection string for design-time operations
        // In real applications, this would be read from configuration
        var connectionString = "Host=localhost;Database=basic_budget;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.MigrationsAssembly("BasicBudget.Infrastructure");
            options.CommandTimeout(60);
        });

        // Create a simple logger for design-time operations
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<BasicBudgetDbContext>();

        return new BasicBudgetDbContext(optionsBuilder.Options, logger);
    }
}