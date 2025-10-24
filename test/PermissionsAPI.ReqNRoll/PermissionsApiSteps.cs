using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Reqnroll;

namespace PermissionsAPI.ReqNRoll;

public class TestWebApplicationFactory : WebApplicationFactory<PermissionsApi.Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting("Logging:LogLevel:Default", "Warning");
        builder.UseSetting("Logging:LogLevel:Microsoft", "Warning");
        builder.UseSetting("Logging:LogLevel:Microsoft.AspNetCore", "Warning");
    }
}

[Binding]
public class PermissionsApiSteps(TestWebApplicationFactory factory)
    : PQSoft.ReqNRoll.ApiStepDefinitions(factory.CreateClient());
