using System.Data;
using System.Text.Json;
using Dapper;
using MySqlConnector;
using PermissionsApi.Models;

namespace PermissionsApi.Services;

public class MySqlHistoryService(string connectionString, TimeProvider timeProvider) : IHistoryService
{
    private async Task<IDbConnection> GetConnectionAsync()
    {
        var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }

    public async Task RecordChangeAsync(string changeType, string entityType, string entityId, IEntity entityAfterChange, string? principal = null, string? reason = null)
    {
        using var connection = await GetConnectionAsync();
        
        const string sql = """
            INSERT INTO history (change_type, entity_type, entity_id, entity_after_change, changed_at, changed_by, reason)
            VALUES (@ChangeType, @EntityType, @EntityId, @EntityAfterChange, @ChangedAt, @ChangedBy, @Reason)
            """;
        
        var entityJson = entityAfterChange != null ? JsonSerializer.Serialize(entityAfterChange) : null;
        
        await connection.ExecuteAsync(sql, new
        {
            ChangeType = changeType,
            EntityType = entityType,
            EntityId = entityId,
            EntityAfterChange = entityJson,
            ChangedAt = timeProvider.GetUtcNow().DateTime,
            ChangedBy = principal,
            Reason = reason
        });
    }

    public async Task<List<HistoryEntry>> GetHistoryAsync(int? skip = null, int? count = null)
    {
        using var connection = await GetConnectionAsync();
        
        const string sql = """
            SELECT change_type as ChangeType, 
                   entity_type as EntityType, 
                   entity_id as EntityId, 
                   entity_after_change as EntityAfterChangeJson,
                   changed_at as TimestampUtc,
                   changed_by as Principal,
                   reason as Reason
            FROM history 
            ORDER BY changed_at DESC 
            LIMIT @Count OFFSET @Skip
            """;
        
        var results = await connection.QueryAsync(sql, new { Skip = skip ?? 0, Count = count ?? 10 });
        
        return results.Select(r => new HistoryEntry(
            r.TimestampUtc,
            r.ChangeType,
            r.EntityType,
            r.EntityId,
            r.EntityAfterChangeJson != null 
                ? JsonSerializer.Deserialize<object>(r.EntityAfterChangeJson) as IEntity ?? new EmptyEntity()
                : new EmptyEntity(),
            r.Principal,
            r.Reason
        )).ToList();
    }

    public async Task<List<HistoryEntry>> GetEntityHistoryAsync(string entityType, string entityId)
    {
        using var connection = await GetConnectionAsync();
        
        const string sql = """
            SELECT change_type as ChangeType, 
                   entity_type as EntityType, 
                   entity_id as EntityId, 
                   entity_after_change as EntityAfterChangeJson,
                   changed_at as TimestampUtc,
                   changed_by as Principal,
                   reason as Reason
            FROM history 
            WHERE entity_type = @EntityType AND entity_id = @EntityId
            ORDER BY changed_at DESC
            """;
        
        var results = await connection.QueryAsync(sql, new { EntityType = entityType, EntityId = entityId });
        
        return results.Select(r => new HistoryEntry(
            r.TimestampUtc,
            r.ChangeType,
            r.EntityType,
            r.EntityId,
            r.EntityAfterChangeJson != null 
                ? JsonSerializer.Deserialize<object>(r.EntityAfterChangeJson) as IEntity ?? new EmptyEntity()
                : new EmptyEntity(),
            r.Principal,
            r.Reason
        )).ToList();
    }
}
