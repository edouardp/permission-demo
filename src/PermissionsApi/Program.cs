using PermissionsApi.Database;
using PermissionsApi.Services;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace PermissionsApi
{
    public class Program
    {
        private Program() { } // Private constructor to satisfy S1118
        
        public static readonly LoggingLevelSwitch LevelSwitch = new();
        
        public static void Main(string[] args)
        {
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(LevelSwitch)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "PermissionsApi");

            var seqUrl = Environment.GetEnvironmentVariable("SEQ_URL");
            if (!string.IsNullOrEmpty(seqUrl))
            {
                loggerConfig.WriteTo.Seq(seqUrl);
            }
            else
            {
                loggerConfig.WriteTo.Console(new CompactJsonFormatter());
            }
            
            Log.Logger = loggerConfig.CreateLogger();

            if (Environment.GetEnvironmentVariable("SUPPRESS_LOGGING") == "true")
            {
                LevelSwitch.MinimumLevel = LogEventLevel.Fatal;
            }

            try
            {
                Log.Information("Starting PermissionsApi");
                
                var builder = WebApplication.CreateBuilder(args);
                builder.Host.UseSerilog();
                builder.Services.AddSingleton(TimeProvider.System);

                // Configure database implementation
                var useDatabase = builder.Configuration.GetValue<bool>("UseDatabase", false);
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

                if (useDatabase && !string.IsNullOrEmpty(connectionString))
                {
                    // MySQL implementation
                    Log.Information("Using MySQL database implementation");
                    
                    // Run database migrations
                    var migrationResult = DatabaseMigrator.MigrateDatabase(connectionString);
                    if (!migrationResult.Successful)
                    {
                        var migrationException = new InvalidOperationException($"Database migration failed: {migrationResult.Error}");
                        Log.Fatal(migrationException, "Database migration failed");
                        throw migrationException;
                    }
                    
                    // Register MySQL services
                    builder.Services.AddScoped<IHistoryService>(provider => 
                        new MySqlHistoryService(connectionString, provider.GetRequiredService<TimeProvider>()));
                    builder.Services.AddScoped<IPermissionsRepository>(provider => 
                        new MySqlPermissionsRepository(connectionString, 
                            provider.GetRequiredService<IHistoryService>(), 
                            provider.GetRequiredService<ILogger<MySqlPermissionsRepository>>()));
                    builder.Services.AddScoped<IIntegrityChecker>(provider => 
                        new MySqlIntegrityChecker(connectionString, 
                            provider.GetRequiredService<ILogger<MySqlIntegrityChecker>>()));
                }
                else
                {
                    // In-memory implementation (default)
                    Log.Information("Using in-memory implementation");
                    
                    builder.Services.AddSingleton<IHistoryService, HistoryService>();
                    builder.Services.AddSingleton<PermissionsRepository>();
                    builder.Services.AddSingleton<IPermissionsRepository>(sp => sp.GetRequiredService<PermissionsRepository>());
                    builder.Services.AddSingleton<IIntegrityChecker, IntegrityChecker>();
                }

                builder.Services.AddControllers();
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new() { 
                        Title = "Permissions API", 
                        Version = "v1",
                        Description = "A production-ready REST API for hierarchical permission management with comprehensive audit trails and debugging capabilities."
                    });
                    
                    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    c.IncludeXmlComments(xmlPath);
                });

                var app = builder.Build();

                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.UseSerilogRequestLogging();
                app.UseHttpsRedirection();
                app.UseAuthorization();
                app.MapControllers();

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
