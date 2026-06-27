using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Paperless.Core.Documents.Entities;

namespace Paperless.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuration for the DocumentCustomField join entity (M2M with payload).
/// </summary>
public class DocumentCustomFieldConfiguration : IEntityTypeConfiguration<DocumentCustomField>
{
    public void Configure(EntityTypeBuilder<DocumentCustomField> builder)
    {
        builder.ToTable("DocumentCustomFields");

        builder.HasKey(x => new { x.DocumentId, x.CustomFieldId });

        builder.Property(x => x.Value)
            .HasColumnType("text");

        builder.HasOne(x => x.Document)
            .WithMany(d => d.CustomFields)
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CustomField)
            .WithMany(cf => cf.DocumentCustomFields)
            .HasForeignKey(x => x.CustomFieldId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
