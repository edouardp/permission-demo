namespace PermissionsApi.Models;

public abstract record AuditableRequest
{
    public string? Principal { get; init; }
    public string? Reason { get; init; }
}
