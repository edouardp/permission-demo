using System.Reflection;
using System.Text.Json;
using PermissionsApi.Models;

namespace PermissionsApi.Services;

public static class BuildInfoService
{
    public static (GitInfo Git, CiInfo? Ci, BuildInfo? Build, AssemblyInfo[] Assemblies) GetBuildInfo()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyDir = Path.GetDirectoryName(assembly.Location);
            if (assemblyDir == null) return (GetFallbackGitInfo(), null, null, GetLoadedAssemblies());
            
            var buildInfoPath = Path.Combine(assemblyDir, "build-info.json");
            if (!File.Exists(buildInfoPath)) return (GetFallbackGitInfo(), null, null, GetLoadedAssemblies());
            
            var json = File.ReadAllText(buildInfoPath);
            var buildInfo = JsonSerializer.Deserialize<JsonElement>(json);
            
            var git = ParseGitInfo(buildInfo.GetProperty("git"));
            var ci = ParseCiInfo(buildInfo.GetProperty("ci"));
            var build = ParseBuildInfo(buildInfo.GetProperty("build"));
            var assemblies = GetLoadedAssemblies();
            
            return (git, ci, build, assemblies);
        }
        catch
        {
            return (GetFallbackGitInfo(), null, null, GetLoadedAssemblies());
        }
    }
    
    private static GitInfo GetFallbackGitInfo()
    {
        // Fallback to environment variables or informational version
        var hash = Environment.GetEnvironmentVariable("GIT_HASH") ?? "unknown";
        var branch = Environment.GetEnvironmentVariable("GIT_BRANCH") ?? "unknown";
        var repo = Environment.GetEnvironmentVariable("GIT_REPO") ?? "unknown";
        
        if (hash == "unknown")
        {
            var assembly = Assembly.GetExecutingAssembly();
            var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
            if (informationalVersion.Contains('+'))
            {
                var parts = informationalVersion.Split('+');
                if (parts.Length > 1 && parts[1].Length >= 7)
                {
                    hash = parts[1][..7];
                }
            }
        }
        
        return new GitInfo(hash, null, branch, repo, null, null);
    }
    
    private static GitInfo ParseGitInfo(JsonElement git)
    {
        return new GitInfo(
            git.GetProperty("hash").GetString() ?? "unknown",
            git.GetProperty("fullHash").GetString(),
            git.GetProperty("branch").GetString() ?? "unknown",
            git.GetProperty("repo").GetString() ?? "unknown",
            git.TryGetProperty("tag", out var tag) && tag.ValueKind != JsonValueKind.Null ? tag.GetString() : null,
            git.TryGetProperty("isDirty", out var dirty) ? dirty.GetBoolean() : null
        );
    }
    
    private static CiInfo? ParseCiInfo(JsonElement ci)
    {
        var provider = ci.GetProperty("provider").GetString();
        if (provider == "local") return null;
        
        return new CiInfo(
            provider ?? "unknown",
            ci.TryGetProperty("buildId", out var buildId) && buildId.ValueKind != JsonValueKind.Null ? buildId.GetString() : null,
            ci.TryGetProperty("buildNumber", out var buildNumber) && buildNumber.ValueKind != JsonValueKind.Null ? buildNumber.GetString() : null,
            ci.TryGetProperty("buildUrl", out var buildUrl) && buildUrl.ValueKind != JsonValueKind.Null ? buildUrl.GetString() : null,
            ci.TryGetProperty("pipeline", out var pipeline) && pipeline.ValueKind != JsonValueKind.Null ? pipeline.GetString() : null,
            ci.TryGetProperty("actor", out var actor) && actor.ValueKind != JsonValueKind.Null ? actor.GetString() : null,
            ci.TryGetProperty("ref", out var refProp) && refProp.ValueKind != JsonValueKind.Null ? refProp.GetString() : null
        );
    }
    
    private static BuildInfo ParseBuildInfo(JsonElement build)
    {
        return new BuildInfo(
            build.GetProperty("timestamp").GetString() ?? "unknown",
            build.GetProperty("machine").GetString() ?? "unknown",
            build.GetProperty("os").GetString() ?? "unknown",
            build.GetProperty("arch").GetString() ?? "unknown",
            build.GetProperty("user").GetString() ?? "unknown",
            build.TryGetProperty("dotnetVersion", out var dotnet) ? dotnet.GetString() : null
        );
    }
    
    private static AssemblyInfo[] GetLoadedAssemblies()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .OrderBy(a => a.GetName().Name)
            .Select(a => new AssemblyInfo(
                Name: a.GetName().Name ?? "Unknown",
                Version: a.GetName().Version?.ToString() ?? "Unknown",
                FileVersion: a.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version,
                InformationalVersion: a.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
                Location: Path.GetFileName(a.Location)
            ))
            .ToArray();
    }
}
