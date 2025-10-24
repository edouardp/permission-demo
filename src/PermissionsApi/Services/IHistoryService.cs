using PermissionsApi.Models;

namespace PermissionsApi.Services;

public interface IHistoryService
{
    Task RecordChangeAsync(string changeType, string entityType, string entityId, object entityAfterChange);
    Task<List<HistoryEntry>> GetHistoryAsync(int? skip = null, int? count = null);
    Task<List<HistoryEntry>> GetEntityHistoryAsync(string entityType, string entityId);
}
