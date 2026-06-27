namespace Paperless.Core.Documents.Entities;

/// <summary>
/// Represents a saved view or filter preset for the document list.
/// Maps to the SavedView model from the original paperless-ngx data model.
/// </summary>
public class SavedView : BaseEntity
{
    /// <summary>
    /// The display name of the saved view.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the owner user.
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// The field to sort by in this view.
    /// </summary>
    public string? SortField { get; set; }

    /// <summary>
    /// Whether the sort order should be reversed (descending).
    /// </summary>
    public bool SortReverse { get; set; }

    /// <summary>
    /// JSON-serialized filter rules that define what documents appear in this view.
    /// Contains an array of rule objects with type, value, and operator.
    /// </summary>
    public string? FilterRules { get; set; }

    /// <summary>
    /// Whether this saved view should appear on the dashboard.
    /// </summary>
    public bool ShowInDashboard { get; set; }

    /// <summary>
    /// Whether this saved view should appear in the sidebar.
    /// </summary>
    public bool ShowInSidebar { get; set; }
}
