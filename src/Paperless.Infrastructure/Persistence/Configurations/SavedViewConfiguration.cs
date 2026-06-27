using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Paperless.Core.Documents.Entities;

namespace Paperless.Infrastructure.Persistence.Configurations;

public class SavedViewConfiguration : IEntityTypeConfiguration<SavedView>
{
    public void Configure(EntityTypeBuilder<SavedView> builder)
    {
        builder.ToTable("SavedViews");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.SortField)
            .HasMaxLength(128);

        builder.Property(x => x.FilterRules)
            .HasColumnType("jsonb");

        builder.Property(x => x.ShowInDashboard);

        builder.Property(x => x.ShowInSidebar);

        builder.HasIndex(x => x.Name);
    }
}
