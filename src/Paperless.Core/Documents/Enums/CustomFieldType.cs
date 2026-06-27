namespace Paperless.Core.Documents.Enums;

/// <summary>
/// Represents the type of a custom field value.
/// Maps to the CustomField.type choices in the original paperless-ngx data model.
/// </summary>
public enum CustomFieldType
{
    /// <summary>Plain text value.</summary>
    Text = 1,

    /// <summary>URL value.</summary>
    Url = 2,

    /// <summary>Date value (ISO 8601).</summary>
    Date = 3,

    /// <summary>Boolean value.</summary>
    Boolean = 4,

    /// <summary>Numeric value (integer or decimal).</summary>
    Number = 5,

    /// <summary>Monetary value (amount + currency).</summary>
    Monetary = 6,
}
