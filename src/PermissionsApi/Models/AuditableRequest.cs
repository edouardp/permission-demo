namespace PermissionsApi.Models;

/// <summary>
/// Base class for requests that support audit trail tracking
/// </summary>
public abstract record AuditableRequest
{
    /// <summary>
    /// Who is making this change (e.g., user email, service account)
    /// </summary>
    /// <example>admin@company.com</example>
    public string? Principal { get; init; }
    
    /// <summary>
    /// Why this change is being made (for audit and compliance)
    /// </summary>
    /// <example>Required for new compliance requirements</example>
    public string? Reason { get; init; }
}
