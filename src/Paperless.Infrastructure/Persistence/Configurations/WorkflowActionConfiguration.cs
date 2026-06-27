using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Paperless.Core.Workflows.Entities;

namespace Paperless.Infrastructure.Persistence.Configurations;

public class WorkflowActionConfiguration : IEntityTypeConfiguration<WorkflowAction>
{
    public void Configure(EntityTypeBuilder<WorkflowAction> builder)
    {
        builder.ToTable("WorkflowActions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .IsRequired();

        builder.Property(x => x.Order);

        builder.Property(x => x.ActionParameters)
            .HasColumnType("jsonb");

        builder.HasOne(x => x.Workflow)
            .WithMany(w => w.Actions)
            .HasForeignKey(x => x.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.WorkflowId);
    }
}
