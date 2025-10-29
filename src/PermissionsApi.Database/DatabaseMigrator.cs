using System.Reflection;
using DbUp;
using DbUp.Engine;
using MySqlConnector;

namespace PermissionsApi.Database;

/// <summary>
/// Handles database schema migrations using DbUp
/// </summary>
public static class DatabaseMigrator
{
    /// <summary>
    /// Ensures the database exists and runs all pending migrations
    /// </summary>
    /// <param name="connectionString">MySQL connection string</param>
    /// <returns>Migration result with success status and error details</returns>
    public static DatabaseUpgradeResult MigrateDatabase(string connectionString)
    {
        // Ensure database exists
        EnsureDatabaseExists(connectionString);
        
        // Run migrations
        var upgrader = DeployChanges.To
            .MySqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .LogToConsole()
            .Build();

        return upgrader.PerformUpgrade();
    }
    
    /// <summary>
    /// Creates the database if it doesn't exist
    /// </summary>
    private static void EnsureDatabaseExists(string connectionString)
    {
        var builder = new MySqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database;
        
        // Connect without specifying database
        builder.Database = "";
        var masterConnectionString = builder.ConnectionString;
        
        using var connection = new MySqlConnection(masterConnectionString);
        connection.Open();
        
        using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE IF NOT EXISTS `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";
        command.ExecuteNonQuery();
    }
    
    /// <summary>
    /// Checks if database needs migration
    /// </summary>
    /// <param name="connectionString">MySQL connection string</param>
    /// <returns>True if migrations are needed</returns>
    public static bool IsMigrationNeeded(string connectionString)
    {
        try
        {
            var upgrader = DeployChanges.To
                .MySqlDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
                .Build();

            return upgrader.GetScriptsToExecute().Any();
        }
        catch
        {
            return true; // Assume migration needed if we can't check
        }
    }
}
