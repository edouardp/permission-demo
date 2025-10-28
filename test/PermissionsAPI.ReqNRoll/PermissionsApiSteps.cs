using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using PermissionsApi.TestSupport;
using Reqnroll;

namespace PermissionsAPI.ReqNRoll;

[UsedImplicitly]
public class TestWebApplicationFactory : WebApplicationFactory<PermissionsApi.Program>
{
    private static readonly MySqlTestFixture _fixture = new();
    private static bool _initialized;
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    public TestWebApplicationFactory()
    {
        InitializeFixture().GetAwaiter().GetResult();
    }

    private static async Task InitializeFixture()
    {
        if (_initialized) return;
        
        await _initLock.WaitAsync();
        try
        {
            if (!_initialized)
            {
                await _fixture.InitializeAsync();
                _initialized = true;
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        PermissionsApi.Program.LevelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Fatal;
        
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseDatabase"] = "true",
                ["ConnectionStrings:DefaultConnection"] = _fixture.ConnectionString
            });
        });
    }
}

[Binding]
public class PermissionsApiSteps(TestWebApplicationFactory factory)
    : PQSoft.ReqNRoll.ApiStepDefinitions(factory.CreateClient());
