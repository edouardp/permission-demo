# Permissions API

An API-first permission system with a three-level hierarchy for managing user access control.

## Overview

This system calculates user permissions through three levels of rules:

1. **Default Permissions** - Applied to all users (e.g., `read: true`)
2. **Group Permissions** - ALLOW or DENY rules for groups
3. **User Permissions** - ALLOW or DENY rules for individual users

**Permission Hierarchy:** Default → Group → User (later levels override earlier ones)

Users are identified by email and can belong to 0 to many groups.

## API Endpoints

### Permissions
- `GET /api/permissions/{email}` - Get calculated permissions for a user

### Groups
- `POST /api/groups` - Create a group
- `POST /api/groups/{groupId}/permissions` - Set group-level permission (ALLOW/DENY)
- `DELETE /api/groups/{groupId}` - Delete a group

### Users
- `POST /api/users` - Create a user with group memberships
- `POST /api/users/{email}/permissions` - Set user-level permission (ALLOW/DENY)
- `DELETE /api/users/{email}` - Delete a user

## Quick Start

```bash
# Build the solution
dotnet build

# Run tests
dotnet test

# Run the API
dotnet run --project src/PermissionsApi
```

## Example Usage

```bash
# Create a group
curl -X POST http://localhost:5000/api/groups \
  -H "Content-Type: application/json" \
  -d '{"name": "editors"}'

# Set group permission
curl -X POST http://localhost:5000/api/groups/{groupId}/permissions \
  -H "Content-Type: application/json" \
  -d '{"permission": "write", "access": "ALLOW"}'

# Create user in group
curl -X POST http://localhost:5000/api/users \
  -H "Content-Type: application/json" \
  -d '{"email": "user@example.com", "groups": ["{groupId}"]}'

# Get user permissions
curl http://localhost:5000/api/permissions/user@example.com
```

## Testing

Tests are written using [Reqnroll](https://reqnroll.net/) with [PQSoft.ReqNRoll](https://www.nuget.org/packages/PQSoft.ReqNRoll) for API testing in plain English (Gherkin syntax).

See `test/PermissionsAPI.ReqNRoll/PermissionsApi.feature` for test scenarios.

All tests are idempotent and use unique identifiers (GUIDs) to support parallel execution.

## Project Structure

```
permissions2/
├── src/
│   └── PermissionsApi/
│       ├── Controllers/       # API controllers
│       ├── Models/           # Data models
│       ├── Services/         # Business logic (PermissionsRepository)
│       └── Program.cs        # Application entry point
├── test/
│   └── PermissionsAPI.ReqNRoll/
│       ├── PermissionsApi.feature    # BDD test scenarios
│       └── PermissionsApiSteps.cs    # Test step definitions
└── PermissionsApi.sln
```

## Technology Stack

- **ASP.NET Core 8.0** - Web API framework
- **Reqnroll** - BDD testing framework
- **PQSoft.ReqNRoll** - API testing library
- **xUnit** - Test runner

## License

MIT
