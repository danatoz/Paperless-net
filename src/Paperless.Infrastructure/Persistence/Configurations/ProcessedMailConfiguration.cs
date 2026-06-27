using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Paperless.Core.Mail.Entities;

namespace Paperless.Infrastructure.Persistence.Configurations;

public class ProcessedMailConfiguration : IEntityTypeConfiguration<ProcessedMail>
{
    public void Configure(EntityTypeBuilder<ProcessedMail> builder)
    {
        builder.ToTable("ProcessedMails");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Received)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(x => x.MailRule)
            .WithMany(r => r.ProcessedMails)
            .HasForeignKey(x => x.MailRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.MailRuleId);
        builder.HasIndex(x => x.Status);
    }
}
