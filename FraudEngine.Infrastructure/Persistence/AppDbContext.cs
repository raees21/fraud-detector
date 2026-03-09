using FraudEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FraudEngine.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<FraudEvaluation> FraudEvaluations { get; set; } = null!;
    public DbSet<RuleDefinition> RuleDefinitions { get; set; } = null!;
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;
    public DbSet<ProcessedIntegrationEvent> ProcessedIntegrationEvents { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Transaction>()
            .Property(transaction => transaction.TransactionType)
            .HasConversion<string>()
            .HasMaxLength(32);

        modelBuilder.Entity<Transaction>()
            .Property(transaction => transaction.ProcessingStatus)
            .HasConversion<string>()
            .HasMaxLength(32);

        modelBuilder.Entity<OutboxMessage>()
            .Property(message => message.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        modelBuilder.Entity<FraudEvaluation>()
            .HasOne(evaluation => evaluation.Transaction)
            .WithOne()
            .HasForeignKey<FraudEvaluation>(evaluation => evaluation.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
