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
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(LevelSwitch)
                .WriteTo.Console(new CompactJsonFormatter())
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "PermissionsApi")
                .CreateLogger();

            if (Environment.GetEnvironmentVariable("SUPPRESS_LOGGING") == "true")
            {
                LevelSwitch.MinimumLevel = LogEventLevel.Fatal;
            }

            try
            {
                Log.Information("Starting PermissionsApi");
                
                var builder = WebApplication.CreateBuilder(args);

                builder.Host.UseSerilog();

                builder.Services.AddSingleton<IPermissionsRepository, PermissionsRepository>();
                builder.Services.AddSingleton(TimeProvider.System);
                builder.Services.AddSingleton<IHistoryService, HistoryService>();
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
