using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Paperless.Core.Workflows.Entities;

namespace Paperless.Infrastructure.Persistence.Configurations;

public class WorkflowTriggerConfiguration : IEntityTypeConfiguration<WorkflowTrigger>
{
    public void Configure(EntityTypeBuilder<WorkflowTrigger> builder)
    {
        builder.ToTable("WorkflowTriggers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .IsRequired();

        builder.Property(x => x.Match)
            .HasMaxLength(256);

        builder.Property(x => x.MatchingAlgorithm);

        builder.Property(x => x.IsInsensitive);

        builder.Property(x => x.FilterRules)
            .HasColumnType("jsonb");

        builder.Property(x => x.FilterPath)
            .HasMaxLength(1024);

        builder.Property(x => x.FilterFilename)
            .HasMaxLength(256);

        builder.Property(x => x.Sources)
            .HasColumnType("jsonb");

        builder.Property(x => x.ScheduleOffsetDays);

        builder.Property(x => x.ScheduleIsRecurring);

        builder.Property(x => x.ScheduleRecurringIntervalDays);

        builder.HasOne(x => x.Workflow)
            .WithMany(w => w.Triggers)
            .HasForeignKey(x => x.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.WorkflowId);
    }
}
