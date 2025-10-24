# Permissions API

A production-ready REST API for hierarchical permission management with comprehensive audit trails and debugging capabilities.

## What It's For

This API provides fine-grained access control for applications requiring complex permission hierarchies. Use it when you need:

- **Multi-tenant applications** with role-based access control
- **Enterprise systems** requiring audit trails and compliance tracking
- **Microservices architectures** needing centralized permission management
- **Applications** with dynamic permission assignment and group-based inheritance

## Architecture

**Three-Level Permission Hierarchy:**
1. **Default Permissions** - System-wide defaults (e.g., `read: true`)
2. **Group Permissions** - ALLOW/DENY rules for user groups
3. **User Permissions** - Individual user overrides

**Resolution Order:** Default → Group → User (later levels override earlier ones)

## Core Features

### Permission Management
- Create/update/delete permissions with default status
- Batch permission operations for performance
- Individual and bulk permission assignment

### User & Group Management
- Users identified by email with multi-group membership
- Groups with hierarchical permission inheritance
- REST-compliant endpoints with proper HTTP semantics

### Audit & Debugging
- Complete change history with UTC timestamps
- Entity-specific history tracking
- Permission resolution chain debugging
- Paginated history endpoints for large datasets

## API Examples

### Basic Permission Setup
```bash
# Create a default permission
curl -X POST /api/v1/permissions \
  -H "Content-Type: application/json" \
  -d '{"name": "read", "description": "Read access", "isDefault": true}'

# Create a group
curl -X POST /api/v1/groups \
  -H "Content-Type: application/json" \
  -d '{"name": "editors"}'
# Returns: {"id": "abc123", "name": "editors"}

# Set group permissions (batch)
curl -X PUT /api/v1/groups/abc123/permissions \
  -H "Content-Type: application/json" \
  -d '{
    "permissions": [
      {"permission": "write", "access": "ALLOW"},
      {"permission": "delete", "access": "DENY"}
    ]
  }'
```

### User Management
```bash
# Create user with group membership
curl -X POST /api/v1/users \
  -H "Content-Type: application/json" \
  -d '{"email": "user@example.com", "groups": ["abc123"]}'

# Override permission at user level
curl -X PUT /api/v1/users/user@example.com/permissions/delete \
  -H "Content-Type: application/json" \
  -d '{"permission": "delete", "access": "ALLOW"}'

# Get calculated permissions
curl /api/v1/users/user@example.com/permissions
# Returns: {"email": "user@example.com", "allow": ["delete", "read", "write"], "deny": []}
```

### Debugging & Audit
```bash
# Debug permission resolution chain
curl /api/v1/user/user@example.com/debug
# Returns detailed chain showing Default → Group → User resolution

# View change history (paginated)
curl "/api/v1/history?skip=0&count=10"

# Entity-specific history
curl /api/v1/permissions/write/history
curl /api/v1/users/user@example.com/history
curl /api/v1/groups/abc123/history
```

## Permission Resolution Example

Given:
- Default: `read: true`
- Group "editors": `write: ALLOW, delete: DENY`  
- User override: `delete: ALLOW`

**Final permissions:** `read: true, write: true, delete: true`

**Debug output shows:**
```json
{
  "permission": "delete",
  "finalResult": "ALLOW",
  "chain": [
    {"level": "Default", "source": "system", "action": "NONE"},
    {"level": "Group", "source": "editors", "action": "DENY"},
    {"level": "User", "source": "user@example.com", "action": "ALLOW"}
  ]
}
```

## Key Endpoints

### Permissions
- `GET /api/v1/permissions` - List all permissions
- `POST /api/v1/permissions` - Create permission
- `PUT /api/v1/permissions/{name}` - Update permission
- `DELETE /api/v1/permissions/{name}` - Delete permission
- `PUT /api/v1/permissions/{name}/default` - Toggle default status

### Groups
- `POST /api/v1/groups` - Create group
- `PUT /api/v1/groups/{id}/permissions` - Batch set permissions
- `PUT /api/v1/groups/{id}/permissions/{name}` - Set individual permission
- `DELETE /api/v1/groups/{id}/permissions/{name}` - Remove permission

### Users
- `POST /api/v1/users` - Create user
- `GET /api/v1/users/{email}/permissions` - Get calculated permissions
- `PUT /api/v1/users/{email}/permissions` - Batch set permissions
- `PUT /api/v1/users/{email}/permissions/{name}` - Set individual permission

### Debugging & History
- `GET /api/v1/user/{email}/debug` - Permission resolution chain
- `GET /api/v1/history?skip=0&count=10` - Global history (paginated)
- `GET /api/v1/permissions/{name}/history` - Permission history
- `GET /api/v1/users/{email}/history` - User history
- `GET /api/v1/groups/{id}/history` - Group history

## Development

```bash
# Build and test
dotnet build
dotnet test

# Run API
dotnet run --project src/PermissionsApi
# API available at http://localhost:5000
```

## Testing

Comprehensive BDD tests using [Reqnroll](https://reqnroll.net/) with plain English scenarios:

- **Permission CRUD operations** with validation
- **Group and user management** with inheritance testing
- **Default permission behavior** and toggles
- **Batch operations** and individual permission management
- **Debug endpoint functionality** with complex scenarios
- **History tracking** with entity-specific filtering
- **Paging functionality** for large datasets

All tests use unique identifiers (GUIDs) for parallel execution and complete isolation.

## Architecture Decisions

- **ASP.NET Core 8.0** with minimal APIs and dependency injection
- **In-memory storage** for simplicity (easily replaceable with database)
- **TimeProvider injection** for testable UTC timestamps
- **Batch operations** to minimize API calls (N operations → 1 call)
- **PUT semantics** for idempotent permission setting
- **RFC 7807 Problem Details** for consistent error responses
- **Alphabetical sorting** for deterministic API responses

## Use Cases

**Enterprise SaaS Platform:**
```bash
# Set up tenant permissions
curl -X POST /api/v1/permissions -d '{"name": "tenant:read", "isDefault": false}'
curl -X POST /api/v1/groups -d '{"name": "tenant-admins"}'
curl -X PUT /api/v1/groups/{id}/permissions -d '{"permissions": [{"permission": "tenant:read", "access": "ALLOW"}]}'
```

**Content Management System:**
```bash
# Editor workflow
curl -X POST /api/v1/groups -d '{"name": "content-editors"}'
curl -X PUT /api/v1/groups/{id}/permissions -d '{
  "permissions": [
    {"permission": "content:create", "access": "ALLOW"},
    {"permission": "content:publish", "access": "DENY"}
  ]
}'
```

**Microservices Authorization:**
```bash
# Service-to-service permissions
curl /api/v1/users/service-account@company.com/permissions
# Use returned permissions for authorization decisions
```

## License

MIT
