using Microsoft.AspNetCore.Mvc.Testing;
using Reqnroll;

namespace PermissionsAPI.ReqNRoll;

[Binding]
public class PermissionsApiSteps(WebApplicationFactory<PermissionsApi.Program> factory)
    : PQSoft.ReqNRoll.ApiStepDefinitions(factory.CreateClient());
