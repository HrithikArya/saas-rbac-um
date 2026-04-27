.PHONY: dev dev-local stop migrate seed test test-backend test-frontend lint build

# ── Docker Dev (all services) ──────────────────────────────────────────────────
dev:
	@cp -n .env.example .env 2>/dev/null || true
	docker compose up --build

stop:
	docker compose down

# ── Local Dev (no Docker — requires local Postgres on localhost:5432) ──────────
dev-local:
	npx --yes concurrently -n backend,frontend -c cyan,magenta \
		"dotnet run --project backend/src/Api" \
		"npm --prefix frontend run dev"

# ── Database ───────────────────────────────────────────────────────────────────
migrate:
	docker compose exec backend dotnet ef database update \
		--project src/Infrastructure \
		--startup-project src/Api

seed:
	docker compose exec backend dotnet run --project tools/Seeder

# ── Testing ────────────────────────────────────────────────────────────────────
test: test-backend test-frontend

test-backend:
	cd backend && dotnet test --configuration Release --logger "console;verbosity=minimal"

test-frontend:
	cd frontend && npm run test

# ── Lint ───────────────────────────────────────────────────────────────────────
lint: lint-backend lint-frontend

lint-backend:
	cd backend && dotnet format --verify-no-changes

lint-frontend:
	cd frontend && npm run lint

# ── Build ──────────────────────────────────────────────────────────────────────
build:
	docker compose build

build-prod:
	docker compose -f docker-compose.prod.yml build

# ── Migrations (create new) ────────────────────────────────────────────────────
migration:
	@read -p "Migration name: " name; \
	docker compose exec backend dotnet ef migrations add $$name \
		--project src/Infrastructure \
		--startup-project src/Api \
		--output-dir Data/Migrations

# ── Helpers ────────────────────────────────────────────────────────────────────
logs:
	docker compose logs -f backend

ps:
	docker compose ps
