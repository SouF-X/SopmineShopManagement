using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SopmineWorkshop.Domain.Fournisseurs;

namespace SopmineWorkshop.Infrastructure.Data.Configurations;

public class FournisseurConfiguration : IEntityTypeConfiguration<Fournisseur>
{
    public void Configure(EntityTypeBuilder<Fournisseur> builder)
    {
        builder.HasKey(f => f.Id).IsClustered(false);

        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.Property(f => f.Nom)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(f => f.ICE)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(f => f.Adresse)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(f => f.Ville)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.TelFix)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(f => f.SiteWeb)
            .HasMaxLength(200);

        builder.Property(f => f.Email)
            .HasMaxLength(100);

        builder.HasMany(f => f.Contacts)
            .WithOne(c => c.Fournisseur)
            .HasForeignKey(c => c.FournisseurId);

        builder.Navigation(f => f.Contacts)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
