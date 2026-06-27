using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Paperless.Core.Documents.Entities;

namespace Paperless.Infrastructure.Persistence.Configurations;

public class CustomFieldConfiguration : IEntityTypeConfiguration<CustomField>
{
    public void Configure(EntityTypeBuilder<CustomField> builder)
    {
        builder.ToTable("CustomFields");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Type)
            .IsRequired();

        // JSON column for ExtraData (stores type-specific configuration)
        builder.Property(x => x.ExtraData)
            .HasColumnType("jsonb");

        builder.HasIndex(x => x.Name).IsUnique();
    }
}
