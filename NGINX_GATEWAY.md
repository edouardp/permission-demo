# Local API Gateway with nginx

Simulate AWS API Gateway locally using nginx to route requests to both services.

## Setup

### Option 1: Docker (Recommended)

```bash
# Start both services
dotnet run --project src/PermissionsApi &
dotnet run --project src/PermissionsSharedBff &

# Start nginx gateway
docker compose up
```

### Option 2: Native nginx

```bash
# Install nginx (macOS)
brew install nginx

# Start both services
dotnet run --project src/PermissionsApi &
dotnet run --project src/PermissionsSharedBff &

# Start nginx with custom config
nginx -c $(pwd)/nginx.conf
```

## Usage

All requests go through `http://localhost:8080`:

```bash
# PermissionsApi (full API)
curl http://localhost:8080/api/v1/permissions

# PermissionsSharedBff (filtered responses)
curl http://localhost:8080/bff/permissions/user/user@example.com

# Health check
curl http://localhost:8080/health
```

## Routing Rules

- `/api/*` → PermissionsApi (port 5000)
- `/bff/*` → PermissionsSharedBff (port 5017)
- `/health` → nginx health check

## Stop Services

```bash
# Stop nginx
docker compose down
# or
nginx -s stop

# Stop .NET services
pkill -f PermissionsApi
pkill -f PermissionsSharedBff
```
