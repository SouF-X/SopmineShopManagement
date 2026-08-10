using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SopmineWorkshop.Domain.Clients;
using SopmineWorkshop.Domain.Fournisseurs;
using SopmineWorkshop.Domain.Invoices;

namespace SopmineWorkshop.Infrastructure.Data.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(invoice => invoice.Id).IsClustered(false);

        builder.Property(invoice => invoice.Id).ValueGeneratedNever();

        builder.Property(invoice => invoice.Reference)
            .HasMaxLength(100)
            .UseCollation("Latin1_General_100_BIN2")
            .IsRequired();

        builder.HasIndex(invoice => invoice.Reference)
            .IsUnique()
            .HasFilter("[Reference] <> N''");

        builder.Property(invoice => invoice.Type)
            .IsRequired();

        builder.Property(invoice => invoice.Nature)
            .IsRequired();

        builder.Property(invoice => invoice.Date)
            .IsRequired();

        builder.Property(invoice => invoice.DueDate);

        builder.Property(invoice => invoice.Status)
            .IsRequired();

        builder.Property(invoice => invoice.PaymentStatus);

        builder.Property(invoice => invoice.PaymentMethod);

        builder.Property(invoice => invoice.PaymentRevision)
            .IsConcurrencyToken()
            .HasDefaultValue(0L);

        builder.Property(invoice => invoice.ConvertedToInvoiceId);

        builder.Property(invoice => invoice.Notes)
            .HasMaxLength(1000);

        builder.Property(invoice => invoice.Subtotal)
            .HasPrecision(18, 2);

        builder.Property(invoice => invoice.TaxTotal)
            .HasPrecision(18, 2);

        builder.Property(invoice => invoice.Total)
            .HasPrecision(18, 2);

        builder.HasMany(invoice => invoice.Lines)
            .WithOne(line => line.Invoice)
            .HasForeignKey(line => line.InvoiceId);

        builder.HasMany(invoice => invoice.Payments)
            .WithOne(payment => payment.Invoice)
            .HasForeignKey(payment => payment.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Fournisseur>()
            .WithMany()
            .HasForeignKey(invoice => invoice.FournisseurId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(invoice => invoice.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
