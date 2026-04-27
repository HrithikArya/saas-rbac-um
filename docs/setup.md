# Setup Guide

## Prerequisites

- .NET 8 SDK
- Node.js 20+
- Git
- PostgreSQL 14+ **or** Docker

---

## Option A — Local dev (no Docker)

### 1. Clone and install frontend deps

```bash
git clone <repo>
cd saas-rbac-um
npm install --prefix frontend
```

### 2. Create the database

```bash
psql -U postgres -c "CREATE DATABASE saas_dev;"
```

### 3. Check config

`backend/src/Api/appsettings.Development.json` is pre-configured for:
- Host: `localhost:5432`
- Database: `saas_dev`
- User: `postgres` / Password: `postgres`

Update credentials if yours differ.

### 4. Start both services

```bash
make dev-local
```

| Service | URL |
|---|---|
| Backend API | http://localhost:5000 |
| Swagger | http://localhost:5000/swagger |
| Frontend | http://localhost:3300 |

### 5. Default accounts

| Email | Password | Role |
|---|---|---|
| `superadmin@localhost` | `SuperAdmin123!` | Super Admin |

The super admin is seeded automatically on first startup.

---

## Option B — Docker (all services)

### 1. Clone

```bash
git clone <repo>
cd saas-rbac-um
```

### 2. Start everything

```bash
make dev
```

This starts Postgres, Redis, Seq, backend, and frontend in one command.

| Service | URL |
|---|---|
| Backend API | http://localhost:5000 |
| Swagger | http://localhost:5000/swagger |
| Frontend | http://localhost:3000 |
| Seq logs | http://localhost:5341 |

---

## Production deployment

```bash
cp .env.example .env
# Fill in: DATABASE_URL, JWT_SECRET, STRIPE_*, RESEND_API_KEY, APP_URL, etc.

make build-prod
docker compose -f docker-compose.prod.yml up -d
```

Nginx listens on ports 80 and 443. To enable HTTPS:
1. Place your cert and key in `docker/nginx/certs/`
2. Add an HTTPS server block to `docker/nginx/nginx.conf`

---

## Common commands

| Command | What it does |
|---|---|
| `make dev-local` | Start backend + frontend locally (no Docker) |
| `make dev` | Start all services via Docker Compose |
| `make stop` | Stop Docker services |
| `make test` | Run backend + frontend tests |
| `make lint` | Check formatting |
| `make migration` | Create a new EF Core migration (Docker) |
| `make logs` | Tail backend logs (Docker) |
