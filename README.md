# Permissions API

A production-ready REST API for hierarchical permission management with comprehensive audit trails, debugging capabilities, and referential integrity checking.

## Table of Contents
- [What It's For](#what-its-for)
- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [Core Features](#core-features)
- [API Reference](#api-reference)
- [Permission Resolution](#permission-resolution)
- [Validation Rules](#validation-rules)
- [Referential Integrity](#referential-integrity)
- [Development](#development)
- [Testing](#testing)
- [Configuration](#configuration)
- [Use Cases](#use-cases)

## What It's For

This API provides fine-grained access control for applications requiring complex permission hierarchies. Use it when you need:

- **Multi-tenant applications** with role-based access control
- **Enterprise systems** requiring audit trails and compliance tracking
- **Microservices architectures** needing centralized permission management
- **Applications** with dynamic permission assignment and group-based inheritance
- **Systems** requiring referential integrity and dependency tracking

## Quick Start

```bash
# Start the API
dotnet run --project src/PermissionsApi
# API available at http://localhost:5000
# Swagger UI at http://localhost:5000/swagger

# Create a permission
curl -X POST http://localhost:5000/api/v1/permissions \
  -H "Content-Type: application/json" \
  -d '{"name": "read", "description": "Read access", "isDefault": true}'

# Create a group
curl -X POST http://localhost:5000/api/v1/groups \
  -H "Content-Type: application/json" \
  -d '{"name": "editors"}'

# Create a user
curl -X POST http://localhost:5000/api/v1/users \
  -H "Content-Type: application/json" \
  -d '{"email": "user@example.com", "groups": ["<group-id>"]}'

# Get user's calculated permissions
curl http://localhost:5000/api/v1/users/user@example.com/permissions
```

## Architecture

### Three-Level Permission Hierarchy

1. **Default Permissions** - System-wide defaults (e.g., `read: true`)
2. **Group Permissions** - ALLOW/DENY rules for user groups (processed alphabetically)
3. **User Permissions** - Individual user overrides (highest priority)

**Resolution Order:** Default → Groups (alphabetically) → User

Later levels override earlier ones. When a user belongs to multiple groups, groups are processed in alphabetical order by name, with later groups overriding earlier ones.

### Data Model

```
Permission
├── name: string (unique identifier)
├── description: string
└── isDefault: boolean

Group
├── id: string (GUID)
├── name: string
└── permissions: Dictionary<string, "ALLOW"|"DENY">

User
├── email: string (unique identifier)
├── groups: List<string> (group IDs)
└── permissions: Dictionary<string, "ALLOW"|"DENY">
```

### Technology Stack

- **ASP.NET Core 9.0** with minimal APIs
- **Serilog** for structured logging (JSON format)
- **In-memory storage** (easily replaceable with database)
- **TimeProvider** for testable timestamps
- **Reqnroll** for BDD integration tests
- **xUnit + AwesomeAssertions** for unit tests

## Core Features

### Permission Management

- Create/update/delete permissions with default status
- Batch permission operations for performance
- Individual and bulk permission assignment
- Dependency tracking to prevent orphaned references
- Validation: `[A-Za-z0-9:-]` (no leading/trailing `:` or `-`, no consecutive `:`, no `:` adjacent to `-`)

### User & Group Management

- Users identified by email with multi-group membership
- Groups with hierarchical permission inheritance
- Alphabetical group processing for deterministic behavior
- REST-compliant endpoints with proper HTTP semantics
- Validation: Group names `[A-Za-z0-9-]`, emails RFC-compliant

### Referential Integrity

- Prevents deletion of permissions assigned to groups or users
- Prevents deletion of groups assigned to users
- Dependency endpoints show what blocks deletion
- Returns HTTP 409 Conflict with detailed error messages

### Audit & Debugging

- Complete change history with UTC timestamps
- Entity-specific history tracking
- Permission resolution chain debugging
- Paginated history endpoints for large datasets
- Optional `principal` and `reason` fields for compliance

## API Reference

### Permissions

#### List All Permissions

```http
GET /api/v1/permissions
```
Returns all permissions sorted alphabetically by name.

#### Create Permission

```http
POST /api/v1/permissions
Content-Type: application/json

{
  "name": "resource:action",
  "description": "Human-readable description",
  "isDefault": false,
  "principal": "admin@company.com",  // Optional
  "reason": "Compliance requirement"  // Optional
}
```

#### Get Permission

```http
GET /api/v1/permissions/{name}
```

#### Update Permission Description

```http
PUT /api/v1/permissions/{name}
Content-Type: application/json

{
  "description": "Updated description",
  "principal": "admin@company.com",
  "reason": "Clarification needed"
}
```

#### Delete Permission

```http
DELETE /api/v1/permissions/{name}
```
Returns 409 Conflict if permission is assigned to any groups or users.

#### Toggle Default Status

```http
PUT /api/v1/permissions/{name}/default
Content-Type: application/json

true  // or false
```

#### Get Permission Dependencies

```http
GET /api/v1/permissions/{name}/dependencies
```
Returns:

```json
{
  "permission": "write",
  "groups": ["editors", "admins"],  // Alphabetically sorted
  "users": ["user1@example.com", "user2@example.com"]  // Alphabetically sorted
}
```

#### Get Permission History

```http
GET /api/v1/permissions/{name}/history
```

### Groups

#### Create Group

```http
POST /api/v1/groups
Content-Type: application/json

{
  "name": "group-name",
  "principal": "admin@company.com",
  "reason": "New team created"
}
```
Returns: `{"id": "guid", "name": "group-name", "permissions": {}}`

#### Set Group Permissions (Batch)

```http
PUT /api/v1/groups/{id}/permissions
Content-Type: application/json

{
  "allow": ["read", "write"],
  "deny": ["delete"],
  "principal": "admin@company.com",
  "reason": "Standard editor permissions"
}
```
Replaces all permissions for the group.

#### Set Individual Group Permission

```http
PUT /api/v1/groups/{id}/permissions/{name}
Content-Type: application/json

{
  "access": "ALLOW",  // or "DENY"
  "principal": "admin@company.com",
  "reason": "Exception granted"
}
```

#### Remove Group Permission

```http
DELETE /api/v1/groups/{id}/permissions/{name}
```

#### Delete Group

```http
DELETE /api/v1/groups/{id}
```
Returns 409 Conflict if group is assigned to any users.

#### Get Group Dependencies

```http
GET /api/v1/groups/{id}/dependencies
```
Returns:
```json
{
  "groupId": "guid",
  "groupName": "editors",
  "users": ["user1@example.com", "user2@example.com"]  // Alphabetically sorted
}
```

#### Get Group History

```http
GET /api/v1/groups/{id}/history
```

### Users

#### Create User

```http
POST /api/v1/users
Content-Type: application/json

{
  "email": "user@example.com",
  "groups": ["group-id-1", "group-id-2"],
  "principal": "admin@company.com",
  "reason": "New employee onboarding"
}
```

#### Get Calculated Permissions

```http
GET /api/v1/users/{email}/permissions
```
Returns:
```json
{
  "email": "user@example.com",
  "allow": ["read", "write"],  // Alphabetically sorted
  "deny": ["delete"]           // Alphabetically sorted
}
```

#### Set User Permissions (Batch)

```http
PUT /api/v1/users/{email}/permissions
Content-Type: application/json

{
  "allow": ["admin"],
  "deny": ["delete"],
  "principal": "admin@company.com",
  "reason": "Temporary admin access"
}
```

#### Set Individual User Permission

```http
PUT /api/v1/users/{email}/permissions/{name}
Content-Type: application/json

{
  "access": "ALLOW",
  "principal": "admin@company.com",
  "reason": "Override group restriction"
}
```

#### Remove User Permission

```http
DELETE /api/v1/users/{email}/permissions/{name}
```

#### Delete User

```http
DELETE /api/v1/users/{email}
```

#### Get User History

```http
GET /api/v1/users/{email}/history
```

### Debugging

#### Debug Permission Resolution

```http
GET /api/v1/user/{email}/debug
```

Returns detailed resolution chain for all permissions:

```json
{
  "email": "user@example.com",
  "permissions": [
    {
      "permission": "delete",
      "finalResult": "ALLOW",
      "chain": [
        {"level": "Default", "source": "system", "action": "NONE"},
        {"level": "Group", "source": "editors", "action": "DENY"},
        {"level": "User", "source": "user@example.com", "action": "ALLOW"}
      ]
    }
  ]
}
```

### History & Audit

#### Get Global History

```http
GET /api/v1/history?skip=0&count=10
```
Returns paginated history of all changes across all entities.

## Permission Resolution

### Resolution Algorithm

1. Start with default permissions (all permissions where `isDefault: true`)
2. Apply group permissions in alphabetical order by group name
3. Apply user-specific permission overrides
4. Return final ALLOW/DENY lists

### Example Scenario

**Setup:**

- Permission `read` with `isDefault: true`
- Permission `write` with `isDefault: false`
- Permission `delete` with `isDefault: false`
- Group `admins`: `write: ALLOW, delete: ALLOW`
- Group `restricted`: `delete: DENY`
- User `user@example.com` in groups `["admins", "restricted"]`
- User override: `delete: ALLOW`

**Resolution:**

1. **Default**: `read: ALLOW`
2. **Group "admins"** (alphabetically first): `read: ALLOW, write: ALLOW, delete: ALLOW`
3. **Group "restricted"** (alphabetically second): `read: ALLOW, write: ALLOW, delete: DENY`
4. **User override**: `read: ALLOW, write: ALLOW, delete: ALLOW`

**Final Result:** `allow: ["delete", "read", "write"], deny: []`

## Validation Rules

### Permission Names

- Pattern: `[A-Za-z0-9:-]+`
- Cannot start or end with `:` or `-`
- No consecutive colons (`::`)
- No hyphen adjacent to colon (`:-` or `-:`)
- Examples: `read`, `user:write`, `admin:delete-all`, `system:a1-b2:c3`

### Group Names

- Pattern: `[A-Za-z0-9-]+`
- Cannot start or end with `-`
- Examples: `editors`, `content-editors`, `team-123`

### Email Addresses

- Standard RFC-compliant email validation
- No consecutive dots in domain
- TLD must be at least 2 characters
- Examples: `user@example.com`, `first.last@company.org`

## Referential Integrity

### Dependency Checking

Before deleting entities, the API checks for dependencies:

**Permission Deletion:**
- Blocked if assigned to any groups (returns 409 with group names)
- Blocked if assigned to any users (returns 409 with user emails)

**Group Deletion:**
- Blocked if assigned to any users (returns 409 with user emails)

### Error Response Format

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Referential integrity violation",
  "status": 409,
  "detail": "Permission is used by groups: editors, admins"
}
```

### Dependency Endpoints

Use dependency endpoints to check what needs cleanup before deletion:

```bash
# Check permission dependencies
curl /api/v1/permissions/write/dependencies

# Check group dependencies
curl /api/v1/groups/{id}/dependencies
```

## Development

### Prerequisites

- .NET 9.0 SDK
- Optional: Seq for log aggregation

### Build and Run

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run API (stdout logging)
dotnet run --project src/PermissionsApi

# Run API with Seq logging
docker run -d --name seq -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest
SEQ_URL=http://localhost:5341 dotnet run --project src/PermissionsApi
```

### Project Structure

```
src/PermissionsApi/
├── Controllers/          # API endpoints
├── Models/              # Request/response models
├── Services/            # Business logic
│   ├── PermissionsRepository.cs
│   ├── HistoryService.cs
│   ├── IntegrityChecker.cs
│   └── Validators/
└── Program.cs

test/
├── PermissionsApi.UnitTests/      # 148 unit tests (xUnit + AwesomeAssertions)
└── PermissionsAPI.ReqNRoll/       # 33 integration tests (Reqnroll BDD)

notebooks/
└── PermissionsAPI.ipynb           # Jupyter notebook for API exploration
```

## Testing

### Unit Tests (148 tests)

- **PermissionNameValidator**: 17 tests covering all validation rules
- **GroupNameValidator**: 10 tests for group name patterns
- **EmailValidator**: 11 tests for email validation
- **IntegrityChecker**: 27 tests for dependency tracking and referential integrity

Run: `dotnet test --filter "FullyQualifiedName~UnitTests"`

### Integration Tests (33 tests)

- **Permissions**: CRUD operations and validation
- **Groups**: Creation, permission assignment, deletion
- **Users**: Creation, group membership, permission calculation
- **Default Permissions**: Toggle behavior and inheritance
- **Debug**: Resolution chain verification
- **History**: Audit trail and pagination
- **Dependencies**: Referential integrity and dependency tracking
- **Group Ordering**: Alphabetical processing verification

Run: `dotnet test --filter "FullyQualifiedName~ReqNRoll"`

### Test Features

- All tests use GUIDs for isolation and parallel execution
- Idempotent tests with cleanup steps
- BDD scenarios in plain English (Gherkin syntax)
- Comprehensive edge case coverage

## Configuration

### Environment Variables

- `SEQ_URL` - Seq server URL for log aggregation (optional)
- `SUPPRESS_LOGGING` - Set to `true` to disable logging (used in tests)
- `ASPNETCORE_ENVIRONMENT` - Set to `Development` for Swagger UI

### Logging

**Development (stdout):**
```bash
dotnet run --project src/PermissionsApi
```

**Production (Seq):**
```bash
SEQ_URL=http://seq-server:5341 dotnet run --project src/PermissionsApi
```

**Log Format:** Compact JSON (Serilog)

## Use Cases

### Enterprise SaaS Platform

```bash
# Set up tenant permissions
curl -X POST /api/v1/permissions \
  -d '{"name": "tenant:read", "description": "Read tenant data", "isDefault": false}'

curl -X POST /api/v1/permissions \
  -d '{"name": "tenant:write", "description": "Modify tenant data", "isDefault": false}'

# Create tenant admin group
curl -X POST /api/v1/groups -d '{"name": "tenant-admins"}'

# Assign permissions
curl -X PUT /api/v1/groups/{id}/permissions \
  -d '{"allow": ["tenant:read", "tenant:write"]}'
```

### Content Management System

```bash
# Create content permissions
curl -X POST /api/v1/permissions \
  -d '{"name": "content:create", "isDefault": false}'
curl -X POST /api/v1/permissions \
  -d '{"name": "content:publish", "isDefault": false}'

# Editor workflow (can create but not publish)
curl -X POST /api/v1/groups -d '{"name": "content-editors"}'
curl -X PUT /api/v1/groups/{id}/permissions \
  -d '{"allow": ["content:create"], "deny": ["content:publish"]}'

# Publisher workflow (can do both)
curl -X POST /api/v1/groups -d '{"name": "content-publishers"}'
curl -X PUT /api/v1/groups/{id}/permissions \
  -d '{"allow": ["content:create", "content:publish"]}'
```

### Microservices Authorization

```bash
# Service account permissions
curl -X POST /api/v1/users \
  -d '{"email": "order-service@company.internal", "groups": []}'

curl -X PUT /api/v1/users/order-service@company.internal/permissions \
  -d '{"allow": ["inventory:read", "payment:write"]}'

# Check permissions before API call
PERMS=$(curl /api/v1/users/order-service@company.internal/permissions)
# Use returned permissions for authorization decisions
```

### Temporary Access Grant

```bash
# Grant temporary admin access with audit trail
curl -X PUT /api/v1/users/contractor@example.com/permissions/admin \
  -d '{
    "access": "ALLOW",
    "principal": "security-team@company.com",
    "reason": "Emergency database maintenance - Ticket #12345"
  }'

# Later: Check who granted access
curl /api/v1/users/contractor@example.com/history
```

## Architecture Decisions

- **In-memory storage**: Simplicity and testability (easily replaceable with EF Core)
- **Alphabetical group ordering**: Deterministic behavior without explicit priorities
- **PUT for batch operations**: Idempotent full replacement semantics
- **Referential integrity**: Prevents orphaned references and data inconsistency
- **Dependency endpoints**: Proactive error prevention before deletion attempts
- **RFC 7807 Problem Details**: Standardized error responses
- **TimeProvider injection**: Testable timestamps without mocking DateTime
- **Serilog structured logging**: Machine-readable logs for aggregation
- **AwesomeAssertions**: Readable test assertions with better collection comparison

## License

MIT
