# Version Information

The Permissions API includes a comprehensive version endpoint that provides detailed build and runtime information for debugging, monitoring, and deployment tracking.

## Version Endpoint

```http
GET /api/v1/version
```

Returns detailed version information including assembly details, runtime environment, git metadata, and CI/CD build information.

### Response Format

```json
{
  "version": "1.0.0.0",
  "fileVersion": "1.0.0.0", 
  "informationalVersion": "1.0.0+0c6b207e47f54d57a841c4a94b6fa2bfd2ed683c",
  "runtimeVersion": "9.0.6",
  "frameworkDescription": ".NET 9.0.6",
  "osDescription": "Darwin 25.1.0 Darwin Kernel Version 25.1.0...",
  "git": {
    "hash": "0c6b207",
    "fullHash": "0c6b207e47f54d57a841c4a94b6fa2bfd2ed683c",
    "branch": "main",
    "repo": "permissions-api",
    "tag": "v1.2.3",
    "isDirty": false
  },
  "ci": {
    "buildNumber": "123",
    "buildId": "abc-def-456",
    "pipeline": "deploy-production",
    "agent": "buildkite-agent-1"
  },
  "build": {
    "timestamp": "2024-10-26T08:54:15.903Z",
    "machine": "build-server-01",
    "user": "ci-user"
  },
  "assemblies": {
    "PermissionsApi": "1.0.0.0",
    "Microsoft.AspNetCore": "9.0.6.0"
  }
}
```

## Data Collection

### Assembly Information
- **version**: Assembly version from `AssemblyVersion` attribute
- **fileVersion**: File version from `AssemblyFileVersion` attribute  
- **informationalVersion**: Full version string including git hash from `AssemblyInformationalVersion`

### Runtime Environment
- **runtimeVersion**: .NET runtime version
- **frameworkDescription**: Full framework description
- **osDescription**: Operating system details

### Git Metadata
- **hash**: Short git commit hash (7 characters)
- **fullHash**: Full 40-character commit hash
- **branch**: Current git branch
- **repo**: Repository name
- **tag**: Git tag if on tagged commit
- **isDirty**: Whether working directory has uncommitted changes

### CI/CD Information
- **buildNumber**: Build sequence number
- **buildId**: Unique build identifier
- **pipeline**: Pipeline/workflow name
- **agent**: Build agent identifier

### Build Details
- **timestamp**: Build timestamp in UTC
- **machine**: Build machine hostname
- **user**: User account that triggered build

## CI/CD Integration

### Buildkite Integration

Add these environment variables to your Buildkite pipeline to populate CI information:

```yaml
steps:
  - label: "Build & Deploy"
    command: |
      export CI_BUILD_NUMBER="$BUILDKITE_BUILD_NUMBER"
      export CI_BUILD_ID="$BUILDKITE_BUILD_ID" 
      export CI_PIPELINE="$BUILDKITE_PIPELINE_SLUG"
      export CI_AGENT="$BUILDKITE_AGENT_NAME"
      dotnet build -c Release
      dotnet publish -c Release
    env:
      BUILDKITE_BUILD_NUMBER: "$$BUILDKITE_BUILD_NUMBER"
      BUILDKITE_BUILD_ID: "$$BUILDKITE_BUILD_ID"
      BUILDKITE_PIPELINE_SLUG: "$$BUILDKITE_PIPELINE_SLUG" 
      BUILDKITE_AGENT_NAME: "$$BUILDKITE_AGENT_NAME"
```

### GitHub Actions Integration

```yaml
- name: Build with version info
  run: |
    export CI_BUILD_NUMBER="$GITHUB_RUN_NUMBER"
    export CI_BUILD_ID="$GITHUB_RUN_ID"
    export CI_PIPELINE="$GITHUB_WORKFLOW"
    export CI_AGENT="$RUNNER_NAME"
    dotnet build -c Release
  env:
    GITHUB_RUN_NUMBER: ${{ github.run_number }}
    GITHUB_RUN_ID: ${{ github.run_id }}
    GITHUB_WORKFLOW: ${{ github.workflow }}
    RUNNER_NAME: ${{ runner.name }}
```

### Azure DevOps Integration

```yaml
- task: DotNetCoreCLI@2
  displayName: 'Build with version info'
  inputs:
    command: 'build'
    configuration: 'Release'
  env:
    CI_BUILD_NUMBER: $(Build.BuildNumber)
    CI_BUILD_ID: $(Build.BuildId)
    CI_PIPELINE: $(Build.DefinitionName)
    CI_AGENT: $(Agent.Name)
```

## Environment Variables

The version controller reads these environment variables during build:

| Variable | Description | Example |
|----------|-------------|---------|
| `CI_BUILD_NUMBER` | Sequential build number | `123` |
| `CI_BUILD_ID` | Unique build identifier | `abc-def-456` |
| `CI_PIPELINE` | Pipeline/workflow name | `deploy-production` |
| `CI_AGENT` | Build agent name | `buildkite-agent-1` |

## MSBuild Integration

The version information is embedded at build time using MSBuild properties. Add to your `.csproj`:

```xml
<PropertyGroup>
  <AssemblyVersion>1.0.0.0</AssemblyVersion>
  <FileVersion>1.0.0.0</FileVersion>
  <AssemblyInformationalVersion>1.0.0+$(GitCommitHash)</AssemblyInformationalVersion>
</PropertyGroup>
```

## Usage Examples

### Health Check Integration

```bash
# Check deployment version
curl https://api.example.com/api/v1/version | jq '.informationalVersion'

# Verify git hash matches deployment
DEPLOYED_HASH=$(curl -s https://api.example.com/api/v1/version | jq -r '.git.hash')
echo "Deployed: $DEPLOYED_HASH"
```

### Monitoring Integration

```bash
# Prometheus metrics endpoint could expose version info
curl /metrics | grep version_info

# Log aggregation with version context
curl /api/v1/version | jq '{version: .version, hash: .git.hash, build: .ci.buildNumber}'
```

### Debugging Production Issues

```bash
# Get full deployment context
curl https://api.example.com/api/v1/version | jq '{
  version: .informationalVersion,
  deployed: .build.timestamp,
  commit: .git.fullHash,
  branch: .git.branch,
  buildNumber: .ci.buildNumber
}'
```

## Security Considerations

- Version endpoint is read-only and contains no sensitive data
- Git hashes and build numbers are safe to expose publicly
- Consider rate limiting if exposed to public internet
- Build machine names and user accounts are included - review for sensitive information

## Implementation Notes

- Uses `Assembly.GetEntryAssembly()` for main application assembly version
- Falls back to `Assembly.GetExecutingAssembly()` if entry assembly is null
- Git information extracted using `libgit2sharp` or git CLI
- Build information captured at compile time via MSBuild
- All timestamps in UTC format for consistency
