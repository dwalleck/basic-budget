using BasicBudget.Domain.Entities;
using BasicBudget.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BasicBudget.Infrastructure.Persistence;

public class BasicBudgetDbContext : DbContext
{
    private readonly ILogger<BasicBudgetDbContext> _logger;

    public BasicBudgetDbContext(DbContextOptions<BasicBudgetDbContext> options, ILogger<BasicBudgetDbContext> logger)
        : base(options)
    {
        _logger = logger;
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetCategory> BudgetCategories => Set<BudgetCategory>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations
        modelBuilder.ApplyConfiguration(new AccountConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());
        modelBuilder.ApplyConfiguration(new BudgetConfiguration());
        modelBuilder.ApplyConfiguration(new BudgetCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());

        // Configure schema
        modelBuilder.HasDefaultSchema("public");

        // Add database-level constraints and indexes
        AddGlobalConstraints(modelBuilder);
        AddPerformanceIndexes(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // This should not happen in production as options are injected
            optionsBuilder.UseNpgsql("Host=localhost;Database=basic_budget;Username=postgres;Password=postgres");
        }

        // Enable sensitive data logging in development
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            optionsBuilder.EnableSensitiveDataLogging();
        }

        // Enable detailed errors in development
        optionsBuilder.EnableDetailedErrors();

        // Configure command timeout
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Saving changes to database");

            // Add audit timestamps for entities that support it
            AddAuditTimestamps();

            var result = await base.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Successfully saved {ChangeCount} changes to database", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save changes to database");
            throw;
        }
    }

    private void AddAuditTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditableEntity &&
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var auditableEntity = (IAuditableEntity)entry.Entity;
            var now = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                auditableEntity.CreatedAt = now;
                auditableEntity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                auditableEntity.UpdatedAt = now;
            }
        }
    }

    private static void AddGlobalConstraints(ModelBuilder modelBuilder)
    {
        // Ensure all Money amounts have proper precision
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                {
                    property.SetColumnType("decimal(18,2)");
                }
            }
        }

        // Add check constraints for business rules
        modelBuilder.Entity<Account>()
            .HasCheckConstraint("CK_Account_Balance_Precision", "SCALE(current_balance) <= 2");

        modelBuilder.Entity<Transaction>()
            .HasCheckConstraint("CK_Transaction_Amount_NonZero", "amount != 0");

        modelBuilder.Entity<Budget>()
            .HasCheckConstraint("CK_Budget_DateRange", "start_date < end_date");

        modelBuilder.Entity<BudgetCategory>()
            .HasCheckConstraint("CK_BudgetCategory_AllocatedAmount_Positive", "allocated_amount > 0");
    }

    private static void AddPerformanceIndexes(ModelBuilder modelBuilder)
    {
        // Indexes for common query patterns
        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.TransactionDate)
            .HasDatabaseName("IX_Transaction_TransactionDate");

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => new { t.AccountId, t.TransactionDate })
            .HasDatabaseName("IX_Transaction_Account_Date");

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => new { t.CategoryId, t.TransactionDate })
            .HasDatabaseName("IX_Transaction_Category_Date");

        modelBuilder.Entity<Account>()
            .HasIndex(a => a.AccountNumber)
            .IsUnique()
            .HasDatabaseName("IX_Account_AccountNumber_Unique");

        modelBuilder.Entity<Budget>()
            .HasIndex(b => new { b.StartDate, b.EndDate })
            .HasDatabaseName("IX_Budget_DateRange");

        modelBuilder.Entity<Category>()
            .HasIndex(c => c.ParentCategoryId)
            .HasDatabaseName("IX_Category_Parent");
    }
}

// Interface for entities that support audit timestamps
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}