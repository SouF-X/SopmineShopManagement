using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SopmineWorkshop.Domain.Clients.Contacts;

namespace SopmineWorkshop.Infrastructure.Data.Configurations;

public class ContactClientConfiguration : IEntityTypeConfiguration<ContactClient>
{
    public void Configure(EntityTypeBuilder<ContactClient> builder)
    {
        builder.HasKey(ct => ct.Id).IsClustered(false);

        builder.Property(ct => ct.Id).ValueGeneratedNever();

        builder.Property(ct => ct.ClientId).IsRequired();

        builder.Property(ct => ct.Nom)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ct => ct.Tel)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(ct => ct.Role)
            .IsRequired();

        builder.HasOne(ct => ct.Client)
            .WithMany(c => c.Contacts)
            .HasForeignKey(ct => ct.ClientId);
    }
}