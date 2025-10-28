using System.Net.NetworkInformation;
using PermissionsApi.Database;
using Testcontainers.MySql;
using Xunit;

namespace PermissionsApi.TestSupport;

/// <summary>
/// Collection fixture for MySQL database setup with schema migrations.
/// Manages MySQL container lifecycle and provides connection string to tests.
/// </summary>
public class MySqlTestFixture : IAsyncLifetime
{
    static MySqlTestFixture()
    {
        // Disable ResourceReaper which can cause issues with Podman
        Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true");
    }
    
    private readonly MySqlContainer? mySqlContainer;

    /// <summary>
    /// Gets the connection string for the MySQL database (container or local).
    /// </summary>
    public string ConnectionString { get; private set; } = string.Empty;
    
    /// <summary>
    /// Gets whether the tests are using a local MySQL instance.
    /// </summary>
    public bool UseLocalMySql { get; }

    public MySqlTestFixture()
    {
        UseLocalMySql = IsPortInUse(3306);
        
        if (!UseLocalMySql)
        {
            mySqlContainer = new MySqlBuilder()
                .WithDatabase("permissions_test")
                .WithUsername("testuser")
                .WithPassword("testpass")
                .WithAutoRemove(true)
                .Build();
        }
    }

    public async Task InitializeAsync()
    {
        if (UseLocalMySql)
        {
            // Connect to local MySQL instance
            ConnectionString = "Server=localhost;Port=3306;Database=permissions_test;Uid=root;Pwd=;MaxPoolSize=200;MinPoolSize=10;";
        }
        else
        {
            await mySqlContainer!.StartAsync();
            ConnectionString = mySqlContainer.GetConnectionString() + ";MaxPoolSize=200;MinPoolSize=10;";
        }
        
        // Run database migrations
        var migrationResult = DatabaseMigrator.MigrateDatabase(ConnectionString);
        if (!migrationResult.Successful)
        {
            throw new InvalidOperationException($"Database migration failed: {migrationResult.Error}");
        }
    }

    public async Task DisposeAsync()
    {
        if (mySqlContainer != null)
        {
            await mySqlContainer.DisposeAsync();
        }
    }

    private static bool IsPortInUse(int port)
    {
        try
        {
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();
            
            return tcpConnInfoArray.Any(endpoint => endpoint.Port == port);
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Collection definition for MySQL tests
/// </summary>
[CollectionDefinition("MySQL")]
public class MySqlTestCollection : ICollectionFixture<MySqlTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
