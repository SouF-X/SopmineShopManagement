using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SopmineWorkshop.Domain.Settings;

namespace SopmineWorkshop.Infrastructure.Data.Configurations;

public sealed class DocumentNominationConfiguration : IEntityTypeConfiguration<DocumentNomination>
{
    public void Configure(EntityTypeBuilder<DocumentNomination> builder)
    {
        builder.HasKey(n => n.Id).IsClustered(false);

        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.Property(n => n.Root)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(n => n.DateFormat)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(n => new { n.Nature, n.Type })
            .IsUnique();
    }
}
