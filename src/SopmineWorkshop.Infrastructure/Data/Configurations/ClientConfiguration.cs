using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SopmineWorkshop.Domain.Clients;

namespace SopmineWorkshop.Infrastructure.Data.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasKey(c => c.Id).IsClustered(false);

        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Nom)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.Type)
            .IsRequired();

        builder.Property(c => c.ICE)
            .HasMaxLength(50);

        builder.Property(c => c.Adresse)
            .HasMaxLength(250);

        builder.Property(c => c.Ville)
            .HasMaxLength(100);

        builder.Property(c => c.Tel)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasMany(c => c.Contacts)
            .WithOne(ct => ct.Client)
            .HasForeignKey(ct => ct.ClientId);

        builder.Navigation(c => c.Contacts)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}