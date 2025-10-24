using PermissionsApi.Models;

namespace PermissionsApi.Services;

public class HistoryService(TimeProvider timeProvider) : IHistoryService
{
    private readonly List<HistoryEntry> history = [];

    public Task RecordChangeAsync(string changeType, string entityType, string entityId, IEntity entityAfterChange, string? principal = null, string? reason = null)
    {
        var entry = new HistoryEntry(
            timeProvider.GetUtcNow().DateTime,
            changeType,
            entityType,
            entityId,
            entityAfterChange,
            principal,
            reason);

        history.Add(entry);
        return Task.CompletedTask;
    }

    public Task<List<HistoryEntry>> GetHistoryAsync(int? skip = null, int? count = null)
    {
        IEnumerable<HistoryEntry> query = history.OrderByDescending(h => h.TimestampUtc);
        
        if (skip.HasValue)
            query = query.Skip(skip.Value);
            
        if (count.HasValue)
            query = query.Take(count.Value);
            
        return Task.FromResult(query.ToList());
    }

    public Task<List<HistoryEntry>> GetEntityHistoryAsync(string entityType, string entityId)
    {
        var entityHistory = history
            .Where(h => h.EntityType == entityType && h.EntityId == entityId)
            .OrderByDescending(h => h.TimestampUtc)
            .ToList();
        return Task.FromResult(entityHistory);
    }
}
