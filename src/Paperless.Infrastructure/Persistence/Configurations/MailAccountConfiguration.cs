using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Paperless.Core.Mail.Entities;

namespace Paperless.Infrastructure.Persistence.Configurations;

public class MailAccountConfiguration : IEntityTypeConfiguration<MailAccount>
{
    public void Configure(EntityTypeBuilder<MailAccount> builder)
    {
        builder.ToTable("MailAccounts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.ImapServer)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.ImapPort);

        builder.Property(x => x.Username)
            .HasMaxLength(256);

        builder.Property(x => x.Password)
            .HasMaxLength(1024);

        builder.Property(x => x.OauthId)
            .HasMaxLength(128);

        builder.HasMany(x => x.MailRules)
            .WithOne(r => r.Account)
            .HasForeignKey(r => r.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Name);
    }
}
