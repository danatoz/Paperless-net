using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Paperless.Core.Mail.Entities;

namespace Paperless.Infrastructure.Persistence.Configurations;

public class MailRuleConfiguration : IEntityTypeConfiguration<MailRule>
{
    public void Configure(EntityTypeBuilder<MailRule> builder)
    {
        builder.ToTable("MailRules");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Folder)
            .HasMaxLength(256);

        builder.Property(x => x.FilterRules)
            .HasColumnType("jsonb");

        builder.Property(x => x.ActionType)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasOne(x => x.Account)
            .WithMany(a => a.MailRules)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ProcessedMails)
            .WithOne(pm => pm.MailRule)
            .HasForeignKey(pm => pm.MailRuleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.AccountId);
    }
}
