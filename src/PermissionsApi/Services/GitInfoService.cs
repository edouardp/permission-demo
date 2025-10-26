using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PermissionsApi.Services;

public static class GitInfoService
{
    private static readonly ILogger Logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger(typeof(GitInfoService));

    public static (string Hash, string Branch, string Repo) GetGitInfo()
    {
        try
        {
            Logger.LogDebug("Getting git information");
            // Look for git-info.json next to the assembly
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyDir = Path.GetDirectoryName(assembly.Location);
            if (assemblyDir == null)
            {
                Logger.LogWarning("Could not determine assembly directory for git info");
                return ("unknown", "unknown", "unknown");
            }
            
            var gitInfoPath = Path.Combine(assemblyDir, "git-info.json");
            if (!File.Exists(gitInfoPath))
            {
                Logger.LogDebug("Git info file not found at {GitInfoPath}", gitInfoPath);
                return ("unknown", "unknown", "unknown");
            }
            
            var json = File.ReadAllText(gitInfoPath);
            var gitInfo = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            
            var result = (
                gitInfo?.GetValueOrDefault("hash", "unknown") ?? "unknown",
                gitInfo?.GetValueOrDefault("branch", "unknown") ?? "unknown", 
                gitInfo?.GetValueOrDefault("repo", "unknown") ?? "unknown"
            );
            
            Logger.LogDebug("Retrieved git info: Hash={Hash}, Branch={Branch}, Repo={Repo}", 
                result.Item1, result.Item2, result.Item3);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to get git information");
            return ("unknown", "unknown", "unknown");
        }
    }
}
