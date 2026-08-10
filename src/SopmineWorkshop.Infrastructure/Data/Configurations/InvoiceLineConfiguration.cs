using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SopmineWorkshop.Domain.Invoices;
using SopmineWorkshop.Domain.Produits;

namespace SopmineWorkshop.Infrastructure.Data.Configurations;

public sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.HasKey(line => line.Id).IsClustered(false);

        builder.Property(line => line.Id).ValueGeneratedNever();

        builder.Property(line => line.InvoiceId)
            .IsRequired();

        builder.Property(line => line.ProduitId);

        builder.Property(line => line.ProductReference)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(line => line.ProductName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(line => line.ProductFamily)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(line => line.ProductUnit)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(line => line.Quantity)
            .HasPrecision(18, 2);

        builder.Property(line => line.Price)
            .HasPrecision(18, 2);

        builder.Property(line => line.TVA)
            .HasPrecision(18, 2);

        builder.Property(line => line.LineSubtotal)
            .HasPrecision(18, 2);

        builder.Property(line => line.LineTax)
            .HasPrecision(18, 2);

        builder.Property(line => line.LineTotal)
            .HasPrecision(18, 2);

        builder.Property(line => line.LineOrder)
            .IsRequired();

        builder.HasOne(line => line.Invoice)
            .WithMany(invoice => invoice.Lines)
            .HasForeignKey(line => line.InvoiceId);

        builder.HasOne<Produit>()
            .WithMany()
            .HasForeignKey(line => line.ProduitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
