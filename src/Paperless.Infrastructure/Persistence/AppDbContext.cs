using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Paperless.Core.Documents.Entities;
using Paperless.Core.Mail.Entities;
using Paperless.Core.Workflows.Entities;

namespace Paperless.Infrastructure.Persistence;

/// <summary>
/// Application EF Core DbContext. Registers all domain entity DbSets
/// and applies entity configurations via the Fluent API.
/// Supports both PostgreSQL and SQLite providers based on <see cref="DatabaseOptions"/>.
/// </summary>
public class AppDbContext : DbContext
{
    private readonly IOptions<DatabaseOptions> _dbOptions;

    public AppDbContext(DbContextOptions<AppDbContext> options, IOptions<DatabaseOptions> dbOptions)
        : base(options)
    {
        _dbOptions = dbOptions;
    }

    // ── Document module entities ─────────────────────────────────────

    /// <summary>
    /// Core document table.
    /// </summary>
    public DbSet<Document> Documents => Set<Document>();

    /// <summary>
    /// Correspondents (senders/creators) of documents.
    /// </summary>
    public DbSet<Correspondent> Correspondents => Set<Correspondent>();

    /// <summary>
    /// Tags for document classification.
    /// </summary>
    public DbSet<Tag> Tags => Set<Tag>();

    /// <summary>
    /// Document type categories (e.g. Invoice, Receipt, Letter).
    /// </summary>
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();

    /// <summary>
    /// Storage path templates for filesystem layout.
    /// </summary>
    public DbSet<StoragePath> StoragePaths => Set<StoragePath>();

    /// <summary>
    /// User-defined custom fields.
    /// </summary>
    public DbSet<CustomField> CustomFields => Set<CustomField>();

    /// <summary>
    /// Join entity for Document ↔ CustomField many-to-many with value storage.
    /// </summary>
    public DbSet<DocumentCustomField> DocumentCustomFields => Set<DocumentCustomField>();

    /// <summary>
    /// Document version history.
    /// </summary>
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();

    /// <summary>
    /// Singleton application configuration.
    /// </summary>
    public DbSet<ApplicationConfiguration> ApplicationConfigurations => Set<ApplicationConfiguration>();

    // ── Workflow module entities ─────────────────────────────────────

    /// <summary>
    /// Workflow definitions.
    /// </summary>
    public DbSet<Workflow> Workflows => Set<Workflow>();

    /// <summary>
    /// Workflow trigger conditions.
    /// </summary>
    public DbSet<WorkflowTrigger> WorkflowTriggers => Set<WorkflowTrigger>();

    /// <summary>
    /// Workflow action definitions.
    /// </summary>
    public DbSet<WorkflowAction> WorkflowActions => Set<WorkflowAction>();

    // ── Task & Sharing entities ──────────────────────────────────────

    /// <summary>
    /// Background task status tracking.
    /// </summary>
    public DbSet<PaperlessTask> PaperlessTasks => Set<PaperlessTask>();

    /// <summary>
    /// Share links for document sharing.
    /// </summary>
    public DbSet<ShareLink> ShareLinks => Set<ShareLink>();

    /// <summary>
    /// Share link bundles.
    /// </summary>
    public DbSet<ShareLinkBundle> ShareLinkBundles => Set<ShareLinkBundle>();

    /// <summary>
    /// Saved view presets.
    /// </summary>
    public DbSet<SavedView> SavedViews => Set<SavedView>();

    // ── Mail module entities ─────────────────────────────────────────

    /// <summary>
    /// Configured mail accounts.
    /// </summary>
    public DbSet<MailAccount> MailAccounts => Set<MailAccount>();

    /// <summary>
    /// Mail processing rules.
    /// </summary>
    public DbSet<MailRule> MailRules => Set<MailRule>();

    /// <summary>
    /// Records of processed emails.
    /// </summary>
    public DbSet<ProcessedMail> ProcessedMails => Set<ProcessedMail>();

    // ── Configuration ────────────────────────────────────────────────

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the Configurations namespace
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Apply the soft-delete global query filter for Document
        modelBuilder.Entity<Document>().HasQueryFilter(x => !x.IsDeleted);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var dbOpts = _dbOptions.Value;

            switch (dbOpts.Provider?.ToLowerInvariant())
            {
                case "postgresql":
                case "postgres":
                    optionsBuilder.UseNpgsql(dbOpts.ConnectionString);
                    break;

                case "sqlite":
                    optionsBuilder.UseSqlite(dbOpts.ConnectionString);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported database provider: '{dbOpts.Provider}'. " +
                        $"Supported values: 'PostgreSQL', 'SQLite'.");
            }
        }
    }
}
