namespace PermissionsApi.Models;

public record HistoryEntry(
    DateTime TimestampUtc,
    string ChangeType,
    string EntityType,
    string EntityId,
    object EntityAfterChange,
    string? Principal = null,
    string? Reason = null)
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
}
