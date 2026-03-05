using FraudEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudEngine.Infrastructure.Persistence.Configurations;

public class RuleDefinitionConfiguration : IEntityTypeConfiguration<RuleDefinition>
{
    public void Configure(EntityTypeBuilder<RuleDefinition> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RuleName).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.Property(r => r.WorkflowJson).HasColumnType("text").IsRequired();

        builder.HasIndex(r => r.RuleName).IsUnique();
    }
}
