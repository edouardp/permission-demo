# Logging Enhancements Summary

This document summarizes the comprehensive logging enhancements added to the PermissionsApi codebase.

## Overview

Extensive logging has been added throughout the entire codebase with careful consideration of log levels:

- **Debug**: Detailed information for troubleshooting, method entry/exit, data validation, internal state
- **Info**: Important business operations, successful completions, configuration changes
- **Warn**: Recoverable issues, missing resources, validation failures
- **Error**: Exceptions and failures that require attention

## Key Features

### 1. Contextual Logging with Serilog.Context

All controllers and services now use `LogContext.PushProperty()` to add contextual information that propagates through the entire call chain:

```csharp
using var _ = LogContext.PushProperty("PermissionName", name);
using var __ = LogContext.PushProperty("Principal", principal);
```

This ensures that high-level context (like permission names, user emails, group IDs, principals) is available in all downstream log messages.

### 2. Enhanced Request Logging

Program.cs now includes enriched request logging that captures:
- Request method, path, status code, and elapsed time
- Host, scheme, user agent, and remote IP
- Custom log levels based on response status and exceptions

### 3. Runtime Log Level Control

The logging system supports runtime log level changes via the `LoggingLevelSwitch`:
- Debug level by default
- Can be suppressed to Fatal level via `SUPPRESS_LOGGING=true` environment variable
- Supports both console (JSON) and Seq logging destinations

## Controller Enhancements

### PermissionController
- **Debug**: Request validation, integrity checks, dependency analysis
- **Info**: CRUD operations, default status changes
- **Warn**: Validation failures, missing resources, integrity violations
- **Error**: Exception handling with full context

### UserController  
- **Debug**: Permission calculations, validation steps, batch operations
- **Info**: User creation, permission assignments, successful operations
- **Warn**: User not found, invalid permissions
- **Error**: Operation failures with context

### GroupController
- **Debug**: Permission validation, batch operations, integrity checks  
- **Info**: Group creation, permission assignments, successful operations
- **Warn**: Group not found, invalid permissions, integrity violations
- **Error**: Operation failures with context

### DebugController
- **Debug**: Debug information generation, permission chain analysis
- **Warn**: User not found for debug
- **Error**: Debug generation failures

### HistoryController
- **Debug**: History retrieval with pagination details
- **Error**: History retrieval failures

### VersionController
- **Debug**: Version information assembly, build info retrieval
- **Error**: Version information failures

## Service Enhancements

### PermissionsRepository
- **Debug**: 
  - Default permission calculations
  - Permission resolution steps
  - Group processing order
  - Debug chain generation
  - Data retrieval operations
- **Info**: All CRUD operations with success confirmations
- **Warn**: Resource not found scenarios
- **Error**: All operation failures with full context

### HistoryService
- **Debug**: 
  - History entry recording with counts
  - Pagination details
  - Query result sizes
- **Error**: History operation failures

### IntegrityChecker
- **Debug**: 
  - Dependency analysis
  - Integrity check results
  - Dependency counts
- **Error**: Integrity check failures

### Validators
- Minimal logging added to avoid excessive noise
- Comments indicate where debug logging could be added if needed for troubleshooting

## Logging Patterns Used

### 1. Contextual Properties
```csharp
using var _ = LogContext.PushProperty("EntityId", id);
using var __ = LogContext.PushProperty("Principal", principal);
```

### 2. Structured Logging
```csharp
logger.LogDebug("Processing {GroupCount} groups for user", groups.Count);
logger.LogInformation("Successfully created permission (IsDefault: {IsDefault})", isDefault);
```

### 3. Exception Handling
```csharp
try
{
    // operation
    logger.LogInformation("Successfully completed operation");
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to complete operation");
    throw;
}
```

### 4. Conditional Logging
```csharp
if (result != null)
{
    logger.LogDebug("Found result: {@Result}", result);
}
else
{
    logger.LogWarning("Result not found");
}
```

## Benefits

1. **Comprehensive Observability**: Every operation is logged with appropriate detail
2. **Contextual Information**: High-level context flows through entire call chains
3. **Performance Monitoring**: Request timing and performance metrics
4. **Troubleshooting**: Debug-level information for detailed analysis
5. **Audit Trail**: All business operations logged with principals and reasons
6. **Runtime Control**: Log levels can be adjusted without restarts
7. **Structured Data**: All logs use structured logging for better analysis
8. **Exception Tracking**: Full exception context with operation details

## Usage Examples

### Development/Troubleshooting
Set log level to Debug to see detailed operation flow:
```bash
# All debug information
dotnet run --project src/PermissionsApi
```

### Production
Suppress debug/info logs for performance:
```bash
# Only warnings and errors
SUPPRESS_LOGGING=true dotnet run --project src/PermissionsApi
```

### Log Aggregation
Use Seq for centralized logging:
```bash
# Send logs to Seq
SEQ_URL=http://localhost:5341 dotnet run --project src/PermissionsApi
```

## Log Level Guidelines

- **Debug**: Internal state, validation steps, detailed flow
- **Information**: Business operations, successful completions
- **Warning**: Recoverable issues, missing resources, validation failures  
- **Error**: Exceptions, operation failures, system issues
- **Fatal**: Application termination events

The logging system now provides comprehensive observability while maintaining performance through appropriate log level usage and runtime control.
