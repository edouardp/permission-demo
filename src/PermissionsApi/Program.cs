using PermissionsApi.Services;
using Serilog;
using Serilog.Formatting.Compact;

namespace PermissionsApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(new CompactJsonFormatter())
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "PermissionsApi")
                .CreateLogger();

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
                builder.Services.AddSwaggerGen();

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
