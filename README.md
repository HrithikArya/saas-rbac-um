# SaaS RBAC Boilerplate

Multi-tenant SaaS starter with authentication, role-based access control, billing, and a full Next.js 15 frontend.

---

## Stack

| Layer | Technology |
|---|---|
| Frontend | Next.js 15 (App Router), TypeScript, Tailwind CSS, shadcn/ui, Zustand, React Query |
| Backend | ASP.NET Core 8, EF Core 8, PostgreSQL, Redis (optional) |
| Auth | JWT access tokens (15 min) + rotating refresh tokens (7 days) |
| Email | Resend API |
| Billing | Stripe (or MockBillingService when keys are absent) |
| Logging | Serilog → Seq |
| Tests | xUnit + Testcontainers (backend), Vitest + RTL (frontend) |

---

## Prerequisites

| Requirement | Version | Notes |
|---|---|---|
| .NET SDK | 8.0+ | [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download) |
| Node.js | 18+ | [nodejs.org](https://nodejs.org) |
| PostgreSQL | 14+ | Local install or any hosted instance |
| Redis | Any | **Optional** — app falls back to in-memory store |
| Docker | Any | Required only for integration tests (Testcontainers) |

---

## Project Structure

```
saas-rbac-um/
├── backend/
│   ├── src/
│   │   ├── Domain/          # Entities, enums
│   │   ├── Application/     # Business logic, interfaces, DTOs
│   │   ├── Infrastructure/  # EF Core, email, billing, token store
│   │   └── Api/             # Controllers, middleware, auth policies
│   └── tests/
│       ├── Application.UnitTests/   # Pure logic, in-memory DB
│       └── Api.IntegrationTests/    # Full HTTP tests, Testcontainers
├── frontend/
│   └── src/
│       ├── app/             # Next.js App Router pages
│       ├── components/      # UI components (shadcn/ui + custom)
│       ├── hooks/           # usePermission, useToast
│       ├── lib/             # Axios client, utils
│       ├── stores/          # Zustand auth + org stores
│       └── types/           # Shared TypeScript types
├── docker/
│   └── nginx/               # Nginx config for production
├── docker-compose.yml        # Dev services (postgres, redis, seq)
├── docker-compose.prod.yml
└── Makefile
```

---

## 1 — Backend Setup

### 1.1 Database

Make sure PostgreSQL is running. Default dev credentials:

```
Host: localhost  Port: 5432
Database: saas_dev
Username: postgres  Password: postgres
```

Change them in `backend/src/Api/appsettings.Development.json` → `ConnectionStrings:DefaultConnection`.

### 1.2 Configuration

Edit `backend/src/Api/appsettings.Development.json`:

```json
{
  "Jwt": {
    "Secret": "your-secret-key-at-least-32-characters!!"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=saas_dev;Username=postgres;Password=postgres"
  },
  "App": {
    "Url": "http://localhost:3300"
  },
  "Resend": {
    "ApiKey": "",
    "FromEmail": "noreply@yourdomain.com"
  },
  "Stripe": {
    "SecretKey": "",
    "WebhookSecret": ""
  }
}
```

**Optional services** — leave empty to run without them:
- `Resend:ApiKey` — email is skipped silently if blank (no crashes, just a log warning)
- `Stripe:SecretKey` — billing falls back to `MockBillingService` which returns local fake URLs
- `Redis` — if `REDIS_URL` env var is absent, an in-memory token store is used

All settings can be overridden with environment variables:

| Env var | Config key |
|---|---|
| `JWT_SECRET` | `Jwt:Secret` |
| `DATABASE_URL` | `ConnectionStrings:DefaultConnection` |
| `REDIS_URL` | `ConnectionStrings:Redis` |
| `STRIPE_SECRET_KEY` | `Stripe:SecretKey` |
| `STRIPE_WEBHOOK_SECRET` | `Stripe:WebhookSecret` |

### 1.3 Migrations

First time only — create the initial migration in Visual Studio Package Manager Console:

```powershell
# Tools → NuGet Package Manager → Package Manager Console
Add-Migration InitialCreate -Project Infrastructure -StartupProject Api
```

Migrations apply automatically on startup in Development. To apply manually:

```powershell
Update-Database -Project Infrastructure -StartupProject Api
```

### 1.4 Run the Backend

**Visual Studio:** open `backend/backend.sln`, press F5.

**CLI:**

```bash
cd backend
dotnet run --project src/Api
```

Starts on:
- HTTP → `http://localhost:49841`
- HTTPS → `https://localhost:49840`

> Ports are set in `src/Api/Properties/launchSettings.json`. Change them there if needed.

**Verify it's running:**

```bash
curl http://localhost:49841/health
# → Healthy
```

**Swagger UI:** `http://localhost:49841/swagger`

---

## 2 — Frontend Setup

### 2.1 Install Dependencies

```bash
cd frontend
npm install
```

### 2.2 Environment Variables

Create `frontend/.env.local`:

```env
NEXT_PUBLIC_API_URL=http://localhost:49841
```

> The frontend proxies `/api/*` → `NEXT_PUBLIC_API_URL` via `next.config.ts`. All Axios calls use `/api/...` paths.

### 2.3 Run the Frontend

```bash
cd frontend
npm run dev
```

Starts on **http://localhost:3300**.

---

## 3 — Running Both Together (Quick Start)

Open two terminals:

**Terminal 1 — Backend:**
```bash
cd backend
dotnet run --project src/Api
```

**Terminal 2 — Frontend:**
```bash
cd frontend
npm install   # first time only
npm run dev
```

Then open **http://localhost:3300** in your browser.

---

## 4 — Frontend Pages

| Route | Description |
|---|---|
| `/login` | Sign in |
| `/register` | Create account |
| `/forgot-password` | Request password reset email |
| `/reset-password?token=...` | Set new password (link from email) |
| `/verify-email?token=...` | Confirm email address (link from email) |
| `/dashboard` | Overview — org stats, member count, your role |
| `/settings` | Rename organization |
| `/settings/members` | Member table, invite dialog, role change, remove |
| `/settings/billing` | Current plan, upgrade (Stripe checkout), manage subscription |

---

## 5 — API Summary

Full documentation: [backend/README.md](backend/README.md)

Base URL: `http://localhost:49841`

All authenticated requests require:
```
Authorization: Bearer {accessToken}
```

Organization-scoped requests also require:
```
X-Organization-Id: {orgId}
```

| Endpoint | Auth | Description |
|---|---|---|
| `POST /auth/register` | — | Create account, returns tokens |
| `POST /auth/login` | — | Sign in, returns tokens |
| `POST /auth/refresh` | — | Rotate refresh token |
| `POST /auth/logout` | ✓ | Revoke refresh token |
| `POST /auth/verify-email` | — | Confirm email token |
| `POST /auth/forgot-password` | — | Send reset email |
| `POST /auth/reset-password` | — | Set new password via token |
| `GET /orgs` | ✓ | List your orgs |
| `POST /orgs` | ✓ | Create org (you become Owner) |
| `GET /orgs/{id}` | ✓ | Get org details |
| `PUT /orgs/{id}` | ✓ Admin+ | Rename org |
| `GET /orgs/{id}/members` | ✓ | List members |
| `GET /orgs/{id}/subscription` | ✓ | Current plan/status |
| `POST /orgs/{id}/invites` | ✓ Admin+ | Invite by email |
| `PATCH /members/{id}/role` | ✓ Admin+ | Change member role |
| `DELETE /members/{id}` | ✓ Admin+ | Remove member |
| `POST /invites/accept` | ✓ | Accept invite token |
| `POST /billing/checkout` | ✓ Owner | Stripe checkout URL |
| `POST /billing/portal` | ✓ Owner | Stripe portal URL |
| `POST /webhooks/stripe` | — (HMAC) | Stripe webhook receiver |

---

## 6 — RBAC

| Permission | Owner | Admin | Member | Viewer |
|---|:---:|:---:|:---:|:---:|
| `projects.read` | ✓ | ✓ | ✓ | ✓ |
| `projects.write` | ✓ | ✓ | ✓ | — |
| `members.manage` | ✓ | ✓ | — | — |
| `billing.manage` | ✓ | — | — | — |

- One Owner per org — cannot be demoted or removed
- Invite can set role to Admin / Member / Viewer only
- Frontend hides/disables UI elements based on the current user's role

---

## 7 — Running Tests

### Backend — Unit Tests (no Docker needed)

```bash
cd backend
dotnet test tests/Application.UnitTests
```

### Backend — Integration Tests (requires Docker)

Docker must be running. Testcontainers spins up PostgreSQL + Redis automatically.

```bash
cd backend
dotnet test tests/Api.IntegrationTests
```

### Frontend — Unit Tests

```bash
cd frontend
npm test
```

Uses Vitest + React Testing Library. Currently covers the `usePermission` hook (all 4 roles).

---

## 8 — Email Setup (Resend)

1. Sign up at [resend.com](https://resend.com)
2. Create an API key
3. Add a verified sender domain
4. Set in `appsettings.Development.json`:
   ```json
   "Resend": {
     "ApiKey": "re_xxxxxxxxxxxx",
     "FromEmail": "noreply@yourdomain.com"
   }
   ```

Without a key, the app runs fine — emails are skipped with a log warning. Useful for local dev where you don't need real email flows.

---

## 9 — Billing Setup (Stripe / Mock)

**Dev without Stripe keys:** the `MockBillingService` is active automatically. Checkout and portal return fake local URLs — nothing is charged.

**With Stripe keys:**

1. Get your secret key from [dashboard.stripe.com](https://dashboard.stripe.com) → Developers → API Keys
2. Create Products + Prices for your plans, copy the `price_xxx` IDs
3. Add a webhook in Dashboard → Webhooks → `https://your-domain.com/webhooks/stripe`
4. Copy the webhook signing secret
5. Set in config or env:
   ```json
   "Stripe": {
     "SecretKey": "sk_test_...",
     "WebhookSecret": "whsec_..."
   }
   ```
6. For local webhook testing:
   ```bash
   stripe listen --forward-to localhost:49841/webhooks/stripe
   ```

> **India users:** Stripe is invite-only in India. The `MockBillingService` covers all UI flows. When ready for production, implement `RazorpayGatewayAdapter` behind the same `IBillingService` interface — no other code changes needed.

---

## 10 — Docker Compose (Optional)

Start PostgreSQL, Redis, and Seq (log viewer) together:

```bash
docker-compose up -d
```

Services:
- PostgreSQL → `localhost:5432`
- Redis → `localhost:6379`
- Seq → `http://localhost:5341`

Stop:
```bash
docker-compose down
```
