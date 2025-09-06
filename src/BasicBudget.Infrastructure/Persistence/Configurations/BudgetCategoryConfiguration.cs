using BasicBudget.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BasicBudget.Infrastructure.Persistence.Configurations;

public class BudgetCategoryConfiguration : IEntityTypeConfiguration<BudgetCategory>
{
    public void Configure(EntityTypeBuilder<BudgetCategory> builder)
    {
        builder.ToTable("budget_categories");

        builder.HasKey(bc => bc.Id);

        builder.Property(bc => bc.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(bc => bc.BudgetId)
            .HasColumnName("budget_id")
            .IsRequired();

        builder.Property(bc => bc.CategoryId)
            .HasColumnName("category_id")
            .IsRequired();

        // Configure Money value objects
        builder.OwnsOne(bc => bc.AllocatedAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("allocated_amount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("allocated_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.OwnsOne(bc => bc.SpentAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("spent_amount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("spent_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(bc => bc.AlertThreshold)
            .HasColumnName("alert_threshold")
            .HasColumnType("decimal(5,4)")
            .IsRequired();

        builder.Property(bc => bc.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(bc => bc.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Configure relationships
        builder.HasOne(bc => bc.Budget)
            .WithMany(b => b.BudgetCategories)
            .HasForeignKey(bc => bc.BudgetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bc => bc.Category)
            .WithMany()
            .HasForeignKey(bc => bc.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint - one category per budget
        builder.HasIndex(bc => new { bc.BudgetId, bc.CategoryId })
            .IsUnique()
            .HasDatabaseName("IX_BudgetCategory_Budget_Category_Unique");
    }
}