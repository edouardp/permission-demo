using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Reqnroll;

namespace PermissionsAPI.ReqNRoll;

public class TestWebApplicationFactory : WebApplicationFactory<PermissionsApi.Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        PermissionsApi.Program.LevelSwitch.MinimumLevel = Serilog.Events.LogEventLevel.Fatal;
    }
}

[Binding]
public class PermissionsApiSteps(TestWebApplicationFactory factory)
    : PQSoft.ReqNRoll.ApiStepDefinitions(factory.CreateClient());
