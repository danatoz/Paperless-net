using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Paperless.Core.Documents.Entities;

namespace Paperless.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuration for the singleton ApplicationConfiguration entity.
/// Enforces that only one row exists by marking Id as a constant and adding a check constraint.
/// </summary>
public class ApplicationConfigurationConfiguration : IEntityTypeConfiguration<ApplicationConfiguration>
{
    public void Configure(EntityTypeBuilder<ApplicationConfiguration> builder)
    {
        builder.ToTable("ApplicationConfigurations");

        builder.HasKey(x => x.Id);

        // Singleton enforcement: only one row with Id = 1 allowed
        builder.Property(x => x.Id)
            .HasDefaultValue(1)
            .ValueGeneratedNever();

        builder.Property(x => x.TrashRetentionDays);

        builder.Property(x => x.ConsumeMaxImagePixels);

        builder.Property(x => x.ConsumeMaxFileSize);

        builder.Property(x => x.OcrClean);

        builder.Property(x => x.OcrCleanContinuously);

        builder.Property(x => x.OcrOutputType)
            .HasMaxLength(32);

        builder.Property(x => x.OcrSkipAlreadyDone);

        builder.Property(x => x.OcrLanguage)
            .HasMaxLength(64);

        builder.Property(x => x.DefaultOwnerId);

        builder.Property(x => x.Timezone)
            .HasMaxLength(64);

        builder.Property(x => x.UpdateCheckingEnabled);

        builder.Property(x => x.PdfDpi);

        builder.Property(x => x.JpegQuality);

        // Ensure only one row exists (singleton enforcement)
        builder.ToTable(t => t.HasCheckConstraint("CK_ApplicationConfiguration_Singleton", "\"Id\" = 1"));
    }
}
