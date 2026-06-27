using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Paperless.Core.Documents.Entities;

namespace Paperless.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Document"/> entity.
/// </summary>
public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");

        // ── Keys ────────────────────────────────────────────────────
        builder.HasKey(x => x.Id);

        // ── Properties ──────────────────────────────────────────────
        builder.Property(x => x.Title)
            .HasMaxLength(256);

        builder.Property(x => x.Content)
            .HasColumnType("text");

        builder.Property(x => x.Checksum)
            .HasMaxLength(64);

        builder.Property(x => x.ArchiveChecksum)
            .HasMaxLength(64);

        builder.Property(x => x.Filename)
            .HasMaxLength(1024);

        builder.Property(x => x.StoragePath)
            .HasMaxLength(1024);

        builder.Property(x => x.Created)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.Added)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.Modified)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ArchiveSerialNumber);

        // ── Relationships ───────────────────────────────────────────
        builder.HasOne(x => x.Correspondent)
            .WithMany(c => c.Documents)
            .HasForeignKey(x => x.CorrespondentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.DocumentType)
            .WithMany(dt => dt.Documents)
            .HasForeignKey(x => x.DocumentTypeId)
            .OnDelete(DeleteBehavior.SetNull);

        // M2M: Document ↔ Tag
        builder.HasMany(x => x.Tags)
            .WithMany(t => t.Documents)
            .UsingEntity<Dictionary<string, object>>(
                "DocumentTags",
                j => j.HasOne<Tag>().WithMany().HasForeignKey("TagId"),
                j => j.HasOne<Document>().WithMany().HasForeignKey("DocumentId"),
                j =>
                {
                    j.HasKey("DocumentId", "TagId");
                    j.ToTable("DocumentTags");
                });

        // M2M: Document ↔ CustomField (via DocumentCustomField join entity)
        builder.HasMany(x => x.CustomFields)
            .WithOne(dcf => dcf.Document)
            .HasForeignKey(dcf => dcf.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        // 1:N: Document → DocumentVersion
        builder.HasMany(x => x.DocumentVersions)
            .WithOne(dv => dv.Document)
            .HasForeignKey(dv => dv.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Indexes ─────────────────────────────────────────────────
        builder.HasIndex(x => x.Checksum);
        builder.HasIndex(x => x.Created);
        builder.HasIndex(x => x.Added);
        builder.HasIndex(x => x.CorrespondentId);
        builder.HasIndex(x => x.DocumentTypeId);
        builder.HasIndex(x => x.ArchiveSerialNumber).IsUnique();
    }
}
