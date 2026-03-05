using FraudEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudEngine.Infrastructure.Persistence.Configurations;

public class FraudEvaluationConfiguration : IEntityTypeConfiguration<FraudEvaluation>
{
    public void Configure(EntityTypeBuilder<FraudEvaluation> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Decision).HasConversion<string>().IsRequired();
        builder.Property(e => e.TriggeredRules).HasColumnType("jsonb").IsRequired();

        builder.HasOne(e => e.Transaction)
               .WithOne()
               .HasForeignKey<FraudEvaluation>(e => e.TransactionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.Decision);
        builder.HasIndex(e => e.RiskScore);
    }
}
