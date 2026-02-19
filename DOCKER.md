# Manna Docker Deployment Guide (Unraid)

## Architecture

Three containers running via Docker Compose:

| Container | Image | Port | Purpose |
|---|---|---|---|
| `manna-postgres` | postgres:16 | 5432 | PostgreSQL database |
| `manna-api` | Custom (.NET 10) | 5082 | ASP.NET Core API |
| `manna-next` | Custom (Node 22) | 3000 | Next.js frontend |

---

## Initial Setup

### 1. Clone the repo on Unraid

```bash
cd /mnt/user/appdata
git clone https://YOUR_USERNAME@github.com/keeps1516/MannaHp.git
cd MannaHp
```

> GitHub requires a Personal Access Token (not password). Generate one at https://github.com/settings/tokens with `repo` scope.

### 2. Save git credentials (so you don't re-enter every pull)

```bash
git config --global credential.helper store
```

Next `git pull` will save your token permanently.

### 3. Create the `.env` file

```bash
nano .env
```

Paste and customize:

```env
# Database
DB_PASSWORD=YourStrongPassword123!
DB_PORT=5432

# .NET API
API_PORT=5082
JWT_KEY=a-random-string-at-least-32-characters-long

# CORS - comma-separated origins allowed to call the API
CORS_ORIGINS=http://YOUR_UNRAID_IP:3000,http://localhost:3000,http://next-client:3000

# Next.js Client
NEXT_PORT=3000
# This is the URL the browser uses to reach the API
NEXT_PUBLIC_API_URL=http://YOUR_UNRAID_IP:5082
```

Replace `YOUR_UNRAID_IP` with your Unraid's LAN IP (find it with `hostname -I`).

Save: `Ctrl+O`, `Enter`, `Ctrl+X`

### 4. Build and start

```bash
docker compose up -d --build
```

First build takes a few minutes. Subsequent builds are faster.

### 5. Verify

```bash
docker compose ps
```

All three containers should show `Up`. Access the app at `http://YOUR_UNRAID_IP:3000`.

### 6. Default login

- **Email:** `owner@manna.local`
- **Password:** `MannaOwner123!`

---

## Common Commands

### Start / Stop

```bash
# Start all services
docker compose up -d

# Stop all services (keeps database data)
docker compose down

# Stop and DELETE database data
docker compose down -v
```

### Rebuild after code changes

```bash
# Pull latest code
git pull

# Rebuild everything
docker compose up -d --build

# Rebuild only the Next.js frontend
docker compose up -d --build next-client

# Rebuild only the API
docker compose up -d --build api
```

### Restart without rebuilding

```bash
# Restart just the API (picks up .env changes)
docker compose restart api

# Restart just the frontend
docker compose restart next-client
```

### Rebuild only app containers (keep database running)

```bash
docker compose down api next-client
git pull
docker compose up -d --build api next-client
```

### View logs

```bash
# All services
docker compose logs

# Follow logs in real-time
docker compose logs -f

# Specific service (last 30 lines)
docker compose logs api --tail 30
docker compose logs next-client --tail 30
docker compose logs postgres --tail 30
```

### Database access

```bash
docker exec -it manna-postgres psql -U app -d restaurant
```

### Check environment variables in a container

```bash
docker compose exec api printenv CorsOrigins
docker compose exec api printenv ConnectionStrings__DefaultConnection
```

---

## How the Docker Build Works

### .NET API (`manna-api`)

- Uses a **`Docker` build configuration** that excludes Blazor/MudBlazor
- Blazor code is wrapped in `#if !DOCKER` preprocessor directives in `Program.cs`
- Local development (`dotnet run`) still uses Debug config with Blazor included
- Dockerfile: `src/Server/Dockerfile` (build context is repo root)
- Auto-runs EF Core migrations on startup
- Seeds the owner account on first boot

### Next.js (`manna-next`)

- Built with `output: 'standalone'` in `next.config.ts`
- `NEXT_PUBLIC_API_URL` is a **build arg** — baked into JavaScript at build time
- If you change `NEXT_PUBLIC_API_URL` in `.env`, you must rebuild: `docker compose up -d --build next-client`
- Dockerfile: `src/next-client/Dockerfile`

### PostgreSQL (`manna-postgres`)

- Standard `postgres:16` image
- Data persisted in `pgdata` Docker volume (survives container restarts)
- Health check ensures API waits for database to be ready

---

## Environment Variable Reference

| Variable | Used By | Description |
|---|---|---|
| `DB_PASSWORD` | postgres, api | Database password |
| `DB_PORT` | postgres | Host port for PostgreSQL (default: 5432) |
| `API_PORT` | api | Host port for the API (default: 5082) |
| `JWT_KEY` | api | JWT signing key (min 32 chars) |
| `CORS_ORIGINS` | api | Comma-separated allowed origins |
| `NEXT_PORT` | next-client | Host port for frontend (default: 3000) |
| `NEXT_PUBLIC_API_URL` | next-client | API URL used by browser (build-time) |

**Important:** `NEXT_PUBLIC_API_URL` is baked into the JavaScript at build time. Changing it in `.env` requires rebuilding the `next-client` container. All other variables take effect on restart.

---

## Accessing the App

### From LAN (same WiFi as Unraid)

```
http://YOUR_UNRAID_IP:3000
```

This is what customers on in-store WiFi would use (QR code points here).

### From Tailscale

```
http://YOUR_TAILSCALE_IP:3000
```

Requires Tailscale on both Unraid and the accessing device. Update `.env` with the Tailscale IP in `NEXT_PUBLIC_API_URL` and `CORS_ORIGINS`, then rebuild `next-client`.

---

## Troubleshooting

### Postgres health check fails on first boot

First boot takes longer because PostgreSQL initializes the database. The `docker-compose.yml` has `start_period: 30s` and `retries: 12` to handle this. If it still fails, just run `docker compose up -d` again.

### CORS errors in browser console

1. Check that the origin (URL in your browser's address bar, including port) is listed in `CORS_ORIGINS`
2. Verify the API has the value: `docker compose exec api printenv CorsOrigins`
3. Restart the API: `docker compose restart api`

### "Failed to fetch" when placing orders

- Verify the API is reachable from your browser: navigate to `http://YOUR_API_URL:5082/api/categories`
- Check API logs: `docker compose logs api --tail 30`
- If accessing over HTTP (not HTTPS), the `generateId()` utility handles the `crypto.randomUUID` limitation

### API can't connect to database

Check the connection string: `docker compose exec api printenv ConnectionStrings__DefaultConnection`
The host should be `postgres` (the Docker service name), not `localhost`.

### Need to reset the database

```bash
docker compose down -v
docker compose up -d --build
```

This deletes all data and recreates everything from scratch.
