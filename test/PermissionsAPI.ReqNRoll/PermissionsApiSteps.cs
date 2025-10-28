using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using PermissionsApi.TestSupport;
using Reqnroll;
using Xunit;

namespace PermissionsAPI.ReqNRoll;

[UsedImplicitly]
[Collection("MySQL")]
public class TestWebApplicationFactory : WebApplicationFactory<PermissionsApi.Program>, IAsyncLifetime
{
    private readonly MySqlTestFixture _fixture;

    public TestWebApplicationFactory(MySqlTestFixture fixture)
    {
        _fixture = fixture;
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

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;
}

[Binding]
public class PermissionsApiSteps(TestWebApplicationFactory factory)
    : PQSoft.ReqNRoll.ApiStepDefinitions(factory.CreateClient());
