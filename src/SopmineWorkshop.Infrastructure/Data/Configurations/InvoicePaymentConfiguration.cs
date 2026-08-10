using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SopmineWorkshop.Domain.Invoices;

namespace SopmineWorkshop.Infrastructure.Data.Configurations;

public sealed class InvoicePaymentConfiguration : IEntityTypeConfiguration<InvoicePayment>
{
    public void Configure(EntityTypeBuilder<InvoicePayment> builder)
    {
        builder.HasKey(payment => payment.Id).IsClustered(false);

        builder.Property(payment => payment.Id).ValueGeneratedNever();

        builder.Property(payment => payment.InvoiceId)
            .IsRequired();

        builder.Property(payment => payment.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(payment => payment.PaymentDate)
            .IsRequired();

        builder.Property(payment => payment.Method);

        builder.Property(payment => payment.Reference)
            .HasMaxLength(100);

        builder.Property(payment => payment.Note)
            .HasMaxLength(500);

        builder.Property(payment => payment.CancellationReason)
            .HasMaxLength(500);

        builder.HasIndex(payment => new { payment.InvoiceId, payment.PaymentDate });
    }
}
