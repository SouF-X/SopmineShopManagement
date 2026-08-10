using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SopmineWorkshop.Domain.Produits;

namespace SopmineWorkshop.Infrastructure.Data.Configurations;

public sealed class FamilleProduitConfiguration : IEntityTypeConfiguration<FamilleProduit>
{
    public void Configure(EntityTypeBuilder<FamilleProduit> builder)
    {
        builder.HasKey(f => f.Id).IsClustered(false);

        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.Property(f => f.Libelle)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(f => f.Libelle);
    }
}
