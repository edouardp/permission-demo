using PermissionsApi.Models;

namespace PermissionsApi.Services;

public interface IHistoryService
{
    Task RecordChangeAsync(string changeType, string entityType, string entityId, IEntity entityAfterChange, string? principal = null, string? reason = null);
    Task<List<HistoryEntry>> GetHistoryAsync(int? skip = null, int? count = null);
    Task<List<HistoryEntry>> GetEntityHistoryAsync(string entityType, string entityId);
}
