using FraudEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudEngine.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.AccountId).IsRequired().HasMaxLength(100);
        builder.Property(t => t.Amount).HasColumnType("decimal(18,2)");
        builder.Property(t => t.Currency).IsRequired().HasMaxLength(3);
        builder.Property(t => t.MerchantName).IsRequired().HasMaxLength(200);
        builder.Property(t => t.MerchantCategory).IsRequired().HasMaxLength(50);
        builder.Property(t => t.IPAddress).IsRequired().HasMaxLength(45);
        builder.Property(t => t.DeviceId).IsRequired().HasMaxLength(100);

        builder.HasIndex(t => t.AccountId);
        builder.HasIndex(t => t.Timestamp);
    }
}
