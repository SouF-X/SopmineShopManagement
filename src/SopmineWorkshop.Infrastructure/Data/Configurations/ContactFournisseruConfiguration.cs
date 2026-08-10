using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SopmineWorkshop.Domain.Fournisseurs.ContactsDeFournisseur;

namespace SopmineWorkshop.Infrastructure.Data.Configurations;

public class ContactFournisseurConfiguration : IEntityTypeConfiguration<ContactFournisseur>
{
    public void Configure(EntityTypeBuilder<ContactFournisseur> builder)
    {
        builder.HasKey(c => c.Id).IsClustered(false);

        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Nom)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Tel)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.Role)
            .IsRequired();

        builder.HasOne(c => c.Fournisseur)
            .WithMany(f => f.Contacts)
            .HasForeignKey(c => c.FournisseurId);
    }
}
