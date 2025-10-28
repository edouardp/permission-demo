using Testcontainers.MySql;
using Xunit;
using PermissionsApi.Database;

namespace PermissionsApi.UnitTests;

public class MySqlTestFixture : IAsyncLifetime
{
    private MySqlContainer? _container;
    
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _container = new MySqlBuilder()
            .WithDatabase("permissions_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
        
        // Run database migrations
        var migrationResult = DatabaseMigrator.MigrateDatabase(ConnectionString);
        if (!migrationResult.Successful)
        {
            throw new InvalidOperationException($"Database migration failed: {migrationResult.Error}");
        }
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }
}

[CollectionDefinition("MySQL")]
public class MySqlTestCollection : ICollectionFixture<MySqlTestFixture>
{
}
