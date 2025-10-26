using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;
using PermissionsApi.Models;
using PermissionsApi.Services;

namespace PermissionsApi.Controllers;

[ApiController]
[Route("api/v1/version")]
public class VersionController : ControllerBase
{
    /// <summary>
    /// Get comprehensive version information including assembly, runtime, git, CI/CD, and build details
    /// </summary>
    /// <returns>Version information</returns>
    /// <response code="200">Version information retrieved successfully</response>
    [HttpGet]
    [ProducesResponseType(typeof(VersionResponse), 200)]
    public VersionResponse GetVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "Unknown";
        var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        
        var (git, ci, build, assemblies) = BuildInfoService.GetBuildInfo();
        
        return new VersionResponse(
            Version: version,
            FileVersion: fileVersion,
            InformationalVersion: informationalVersion,
            RuntimeVersion: Environment.Version.ToString(),
            FrameworkDescription: RuntimeInformation.FrameworkDescription,
            OSDescription: RuntimeInformation.OSDescription,
            Git: git,
            Ci: ci,
            Build: build,
            Assemblies: assemblies
        );
    }
}
