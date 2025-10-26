using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;
using PermissionsApi.Exceptions;
using PermissionsApi.Models;
using PermissionsApi.Services;

namespace PermissionsApi.Controllers;

[ApiController]
[Route("api/v1/version")]
public class VersionController(ILogger<VersionController> logger) : ControllerBase
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
        try
        {
            logger.LogDebug("Getting version information");
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "Unknown";
            var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
            var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            
            var (git, ci, build, assemblies) = BuildInfoService.GetBuildInfo();
            
            var response = new VersionResponse(
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
            
            logger.LogDebug("Successfully retrieved version information: {Version}", version);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get version information");
            throw new OperationException("Operation failed", ex);
        }
    }
}
