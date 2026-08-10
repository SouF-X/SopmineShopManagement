using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using SopmineWorkshop.Domain.Settings;

namespace SopmineWorkshop.Infrastructure.Data.Configurations;

public sealed class DocumentReferenceSequenceConfiguration : IEntityTypeConfiguration<DocumentReferenceSequence>
{
    public void Configure(EntityTypeBuilder<DocumentReferenceSequence> builder)
    {
        builder.HasKey(sequence => sequence.Scope);
        builder.Property(sequence => sequence.Scope)
            .HasMaxLength(80)
            .UseCollation("Latin1_General_100_BIN2")
            .ValueGeneratedNever();
        builder.Property(sequence => sequence.LastSequence)
            .IsRequired();
    }
}
