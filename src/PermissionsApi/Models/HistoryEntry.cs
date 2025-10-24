namespace PermissionsApi.Models;

#pragma warning disable IDE0060 // Remove unused parameter

public record HistoryEntry(

    DateTime TimestampUtc,
    string ChangeType,
    string EntityType,
    string EntityId,
    IEntity EntityAfterChange,
    string? Principal = null,
    string? Reason = null)
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
}

#pragma warning restore IDE0060
