using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Paperless.Core.Documents.Entities;
using Paperless.Core.Mail.Entities;
using Paperless.Core.Workflows.Entities;
using Paperless.Infrastructure.Persistence.Interceptors;

namespace Paperless.Infrastructure.Persistence;

/// <summary>
/// Application EF Core DbContext. Registers all domain entity DbSets
/// and applies entity configurations via the Fluent API.
/// Supports both PostgreSQL and SQLite providers based on <see cref="DatabaseOptions"/>.
/// Automatically applies soft-delete global query filters and audit interceptors.
/// </summary>
public class AppDbContext : DbContext
{
    private readonly IOptions<DatabaseOptions> _dbOptions;
    private readonly SoftDeleteInterceptor _softDeleteInterceptor;
    private readonly AuditInterceptor _auditInterceptor;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IOptions<DatabaseOptions> dbOptions,
        SoftDeleteInterceptor softDeleteInterceptor,
        AuditInterceptor auditInterceptor)
        : base(options)
    {
        _dbOptions = dbOptions;
        _softDeleteInterceptor = softDeleteInterceptor;
        _auditInterceptor = auditInterceptor;
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

        // Apply the soft-delete global query filter for all ISoftDeletable entities.
        // This ensures soft-deleted records are automatically excluded from queries.
        ApplySoftDeleteQueryFilter(modelBuilder);
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

        // Add persistence interceptors.
        // When options are configured externally (via AddDbContext), interceptors
        // are added there. This fallback covers design-time and auto-configuration.
        optionsBuilder.AddInterceptors(_softDeleteInterceptor, _auditInterceptor);
    }

    /// <summary>
    /// Applies the soft-delete global query filter (IsDeleted == false) to all entity types
    /// that implement <see cref="ISoftDeletable"/>. This is done dynamically for every
    /// entity in the model, so adding a new entity that implements <see cref="ISoftDeletable"/>
    /// automatically gets the filter without additional configuration.
    /// </summary>
    private static void ApplySoftDeleteQueryFilter(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            var condition = Expression.Equal(property, Expression.Constant(false));
            var lambda = Expression.Lambda(condition, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
}
