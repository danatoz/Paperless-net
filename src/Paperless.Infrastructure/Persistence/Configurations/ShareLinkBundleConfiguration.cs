using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Paperless.Core.Documents.Entities;

namespace Paperless.Infrastructure.Persistence.Configurations;

public class ShareLinkBundleConfiguration : IEntityTypeConfiguration<ShareLinkBundle>
{
    public void Configure(EntityTypeBuilder<ShareLinkBundle> builder)
    {
        builder.ToTable("ShareLinkBundles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Slug)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Created)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.Expires)
            .HasColumnType("timestamp with time zone");

        builder.HasMany(x => x.Links)
            .WithOne(l => l.ShareLinkBundle)
            .HasForeignKey(l => l.ShareLinkBundleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Slug).IsUnique();
    }
}
