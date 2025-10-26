using System.Reflection;
using System.Text.Json;

namespace PermissionsApi.Services;

public static class GitInfoService
{
    public static (string Hash, string Branch, string Repo) GetGitInfo()
    {
        try
        {
            // Look for git-info.json next to the assembly
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyDir = Path.GetDirectoryName(assembly.Location);
            if (assemblyDir == null) return ("unknown", "unknown", "unknown");
            
            var gitInfoPath = Path.Combine(assemblyDir, "git-info.json");
            if (!File.Exists(gitInfoPath)) return ("unknown", "unknown", "unknown");
            
            var json = File.ReadAllText(gitInfoPath);
            var gitInfo = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            
            return (
                gitInfo?.GetValueOrDefault("hash", "unknown") ?? "unknown",
                gitInfo?.GetValueOrDefault("branch", "unknown") ?? "unknown", 
                gitInfo?.GetValueOrDefault("repo", "unknown") ?? "unknown"
            );
        }
        catch
        {
            return ("unknown", "unknown", "unknown");
        }
    }
}
