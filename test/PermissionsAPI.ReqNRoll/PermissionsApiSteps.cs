using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Reqnroll;

namespace PermissionsAPI.ReqNRoll;

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
                ["UseDatabase"] = "true"
            });
        });
    }
}

[Binding]
public class PermissionsApiSteps(TestWebApplicationFactory factory)
    : PQSoft.ReqNRoll.ApiStepDefinitions(factory.CreateClient());
