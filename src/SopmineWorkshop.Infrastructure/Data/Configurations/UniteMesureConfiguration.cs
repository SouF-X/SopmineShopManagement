using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SopmineWorkshop.Domain.Produits;

namespace SopmineWorkshop.Infrastructure.Data.Configurations;

public sealed class UniteMesureConfiguration : IEntityTypeConfiguration<UniteMesure>
{
    public void Configure(EntityTypeBuilder<UniteMesure> builder)
    {
        builder.HasKey(u => u.Id).IsClustered(false);

        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Libelle)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(u => u.Libelle);
    }
}
