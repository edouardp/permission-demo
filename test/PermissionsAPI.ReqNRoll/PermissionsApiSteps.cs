using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using PermissionsApi.TestSupport;
using Reqnroll;

namespace PermissionsAPI.ReqNRoll;

[Binding]
public class TestHooks
{
    private static MySqlTestFixture? _fixture;
    
    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        _fixture = new MySqlTestFixture();
        await _fixture.InitializeAsync();
    }
    
    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        if (_fixture != null)
            await _fixture.DisposeAsync();
    }

    public static string GetConnectionString() => _fixture?.ConnectionString ?? throw new InvalidOperationException("Fixture not initialized");
}

[UsedImplicitly]
public class TestWebApplicationFactory : WebApplicationFactory<PermissionsApi.Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        PermissionsApi.Program.LevelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Fatal;
        
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseDatabase"] = "true",
                ["ConnectionStrings:DefaultConnection"] = TestHooks.GetConnectionString()
            });
        });
    }
}

[Binding]
public class PermissionsApiSteps(TestWebApplicationFactory factory)
    : PQSoft.ReqNRoll.ApiStepDefinitions(factory.CreateClient());
