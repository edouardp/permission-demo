namespace PermissionsApi.Models;

public record HistoryEntry(
    DateTime TimestampUtc,
    string ChangeType,
    string EntityType,
    string EntityId,
    object EntityAfterChange)
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
}
