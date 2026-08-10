using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SopmineWorkshop.Domain.Produits;

namespace SopmineWorkshop.Infrastructure.Data.Configurations;

public sealed class ProduitConfiguration : IEntityTypeConfiguration<Produit>
{
    public void Configure(EntityTypeBuilder<Produit> builder)
    {
        builder.HasKey(p => p.Id).IsClustered(false);

        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Reference)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Nom)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.Famille)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Unite)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.FournisseurId);

        builder.Property(p => p.ImageUrl)
            .HasColumnType("nvarchar(max)");

        builder.Property(p => p.Quantite)
            .HasPrecision(18, 2);

        builder.Property(p => p.QuantiteMini)
            .HasPrecision(18, 2);

        builder.Property(p => p.PuAchatHT)
            .HasPrecision(18, 2);

        builder.Property(p => p.TVA)
            .HasPrecision(18, 2);

        builder.Property(p => p.Marge)
            .HasPrecision(18, 2);

        builder.Property(p => p.PVenteTTC)
            .HasPrecision(18, 2);

        builder.HasIndex(p => p.Reference);

        builder.HasIndex(p => p.FournisseurId);

        builder.HasOne(p => p.Fournisseur)
            .WithMany()
            .HasForeignKey(p => p.FournisseurId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
