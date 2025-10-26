namespace PermissionsApi.Models;

public record VersionResponse(
    string Version,
    string? FileVersion,
    string? InformationalVersion,
    string RuntimeVersion,
    string FrameworkDescription,
    string OSDescription,
    GitInfo Git,
    CiInfo? Ci,
    BuildInfo? Build,
    AssemblyInfo[] Assemblies
);

public record GitInfo(
    string Hash,
    string? FullHash,
    string Branch,
    string Repo,
    string? Tag,
    bool? IsDirty
);

public record CiInfo(
    string Provider,
    string? BuildId,
    string? BuildNumber,
    string? BuildUrl,
    string? Pipeline,
    string? Actor,
    string? Ref
);

public record BuildInfo(
    string Timestamp,
    string Machine,
    string OS,
    string Arch,
    string User,
    string? DotnetVersion
);

public record AssemblyInfo(
    string Name,
    string Version,
    string? FileVersion,
    string? InformationalVersion,
    string Location
);
