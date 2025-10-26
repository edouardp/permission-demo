using Microsoft.Extensions.Logging;
using PermissionsApi.Models;
using Serilog.Context;

namespace PermissionsApi.Services;

public class HistoryService(TimeProvider timeProvider, ILogger<HistoryService> logger) : IHistoryService
{
    private readonly List<HistoryEntry> history = [];

    public Task RecordChangeAsync(string changeType, string entityType, string entityId, IEntity entityAfterChange, string? principal = null, string? reason = null)
    {
        using var changeTypeContext = LogContext.PushProperty("ChangeType", changeType);
        using var entityTypeContext = LogContext.PushProperty("EntityType", entityType);
        using var entityIdContext = LogContext.PushProperty("EntityId", entityId);
        using var principalContext = LogContext.PushProperty("Principal", principal);
        
        logger.LogDebug("Recording history entry with reason: {Reason}", reason);
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
