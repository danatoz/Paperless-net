using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Paperless.Core.Documents.Entities;

namespace Paperless.Infrastructure.Persistence.Configurations;

public class ShareLinkConfiguration : IEntityTypeConfiguration<ShareLink>
{
    public void Configure(EntityTypeBuilder<ShareLink> builder)
    {
        builder.ToTable("ShareLinks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Slug)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Created)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.Expires)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.FileVersion)
            .IsRequired();

        builder.HasOne(x => x.Document)
            .WithMany()
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ShareLinkBundle)
            .WithMany(b => b.Links)
            .HasForeignKey(x => x.ShareLinkBundleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.DocumentId);
    }
}
