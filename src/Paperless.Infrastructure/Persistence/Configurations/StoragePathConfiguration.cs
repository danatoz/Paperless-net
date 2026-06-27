using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Paperless.Core.Documents.Entities;

namespace Paperless.Infrastructure.Persistence.Configurations;

public class StoragePathConfiguration : IEntityTypeConfiguration<StoragePath>
{
    public void Configure(EntityTypeBuilder<StoragePath> builder)
    {
        builder.ToTable("StoragePaths");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Slug)
            .HasMaxLength(256);

        builder.Property(x => x.PathTemplate)
            .HasMaxLength(1024);

        builder.Property(x => x.Match)
            .HasMaxLength(256);

        builder.Property(x => x.MatchingAlgorithm);

        builder.Property(x => x.IsInsensitive);

        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}
