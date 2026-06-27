using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Paperless.Core.Documents.Entities;

namespace Paperless.Infrastructure.Persistence.Configurations;

public class PaperlessTaskConfiguration : IEntityTypeConfiguration<PaperlessTask>
{
    public void Configure(EntityTypeBuilder<PaperlessTask> builder)
    {
        builder.ToTable("PaperlessTasks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TaskId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(256);

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.Acknowledged);

        builder.Property(x => x.Done)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.Result)
            .HasColumnType("text");

        builder.HasIndex(x => x.TaskId);
        builder.HasIndex(x => x.Status);
    }
}
