using PermissionsApi.Models;

namespace PermissionsApi.Services;

public interface IHistoryService
{
    Task RecordChangeAsync(string changeType, string entityType, string entityId, object entityAfterChange);
    Task<List<HistoryEntry>> GetHistoryAsync(int? skip = null, int? count = null);
    Task<List<HistoryEntry>> GetEntityHistoryAsync(string entityType, string entityId);
}

public class HistoryService : IHistoryService
{
    private readonly TimeProvider _timeProvider;
    private readonly List<HistoryEntry> _history = new();

    public HistoryService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public Task RecordChangeAsync(string changeType, string entityType, string entityId, object entityAfterChange)
    {
        var entry = new HistoryEntry
        {
            TimestampUtc = _timeProvider.GetUtcNow().DateTime,
            ChangeType = changeType,
            EntityType = entityType,
            EntityId = entityId,
            EntityAfterChange = entityAfterChange
        };

        _history.Add(entry);
        return Task.CompletedTask;
    }

    public Task<List<HistoryEntry>> GetHistoryAsync(int? skip = null, int? count = null)
    {
        IEnumerable<HistoryEntry> query = _history.OrderByDescending(h => h.TimestampUtc);
        
        if (skip.HasValue)
            query = query.Skip(skip.Value);
            
        if (count.HasValue)
            query = query.Take(count.Value);
            
        return Task.FromResult(query.ToList());
    }

    public Task<List<HistoryEntry>> GetEntityHistoryAsync(string entityType, string entityId)
    {
        var entityHistory = _history
            .Where(h => h.EntityType == entityType && h.EntityId == entityId)
            .OrderByDescending(h => h.TimestampUtc)
            .ToList();
        return Task.FromResult(entityHistory);
    }
}
