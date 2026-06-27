using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Paperless.Core.Documents.Entities;

namespace Paperless.Infrastructure.Persistence.Configurations;

public class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        builder.ToTable("DocumentVersions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.VersionNumber)
            .IsRequired();

        builder.Property(x => x.FileName)
            .HasMaxLength(1024);

        builder.Property(x => x.FileSize);

        builder.Property(x => x.Timestamp)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(x => x.Document)
            .WithMany(d => d.DocumentVersions)
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.DocumentId, x.VersionNumber }).IsUnique();
    }
}
