# SaaS RBAC Backend

ASP.NET Core 8 Web API implementing multi-tenant authentication and role-based access control (RBAC).

---

## Table of Contents

- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Local Setup](#local-setup)
- [Configuration](#configuration)
- [Running the API](#running-the-api)
- [API Reference](#api-reference)
  - [Auth](#auth-endpoints)
  - [Organizations](#organization-endpoints)
  - [Members](#member-endpoints)
  - [Invites](#invite-endpoints)
  - [Billing](#billing-endpoints)
  - [Webhooks](#webhook-endpoints)
- [RBAC — Roles & Permissions](#rbac--roles--permissions)
- [How the Org Context Header Works](#how-the-org-context-header-works)
- [Token Strategy](#token-strategy)
- [Project Structure](#project-structure)
- [Running Tests](#running-tests)
- [Database Migrations](#database-migrations)

---

## Architecture

```
backend/
├── src/
│   ├── Domain/          # Entities, enums — no dependencies
│   ├── Application/     # Business logic, service interfaces, DTOs
│   ├── Infrastructure/  # EF Core, PostgreSQL, token store, email
│   └── Api/             # Controllers, middleware, auth policies
└── tests/
    ├── Application.UnitTests/    # Pure logic tests, in-memory EF Core
    └── Api.IntegrationTests/     # Full HTTP tests, Testcontainers
```

**Dependency flow:** `Api → Application + Infrastructure → Domain`

---

## Prerequisites

| Requirement | Version | Notes |
|-------------|---------|-------|
| .NET SDK | 8.0+ | [download](https://dotnet.microsoft.com/download) |
| PostgreSQL | 14+ | Local install or hosted |
| Redis | Any | Optional — uses in-memory fallback if absent |
| Visual Studio | 2022+ | Or `dotnet` CLI |

> **No Docker required for local development.** Redis is optional — the API automatically falls back to an in-process in-memory store if `REDIS_URL` is not configured (a warning is logged on startup). Do not use the in-memory store in production.

---

## Local Setup

**1. Clone and open**

Open `backend/backend.sln` in Visual Studio, or `cd backend` in a terminal.

**2. Create the database**

Make sure PostgreSQL is running. The API will auto-create and migrate the database on first startup (Development environment only). Default connection uses:

```
Host=localhost;Port=5432;Database=saas_dev;Username=postgres;Password=postgres
```

Override in `appsettings.Development.json` → `ConnectionStrings:DefaultConnection` if your credentials differ.

**3. Create a migration (first time only)**

In Visual Studio Package Manager Console (Tools → NuGet Package Manager → Package Manager Console):

```powershell
Add-Migration InitialCreate -Project Infrastructure -StartupProject Api
```

After this, migrations run automatically on startup in Development. You can also run them manually:

```powershell
Update-Database -Project Infrastructure -StartupProject Api
```

**4. Run the API**

Press F5 in Visual Studio, or:

```bash
dotnet run --project src/Api
```

Swagger UI is available at `https://localhost:{port}/swagger`.

---

## Configuration

All settings live in `src/Api/appsettings.json` (base) and `appsettings.Development.json` (overrides). Environment variables take priority over config files.

### Required settings

| Setting | Env var | Config key | Default |
|---------|---------|------------|---------|
| JWT signing secret | `JWT_SECRET` | `Jwt:Secret` | *(must be set)* |
| Database connection | `DATABASE_URL` | `ConnectionStrings:DefaultConnection` | see above |
| Redis connection | `REDIS_URL` | `ConnectionStrings:Redis` | *(falls back to in-memory)* |
| Frontend URL (CORS) | `APP_URL` | `App:Url` | `http://localhost:3000` |
| Seq logging server | `SEQ_URL` | `Seq:ServerUrl` | `http://localhost:5341` |
| Stripe secret key | `STRIPE_SECRET_KEY` | `Stripe:SecretKey` | *(required for billing)* |
| Stripe webhook secret | `STRIPE_WEBHOOK_SECRET` | `Stripe:WebhookSecret` | *(required for webhooks)* |

### JWT settings (config only, not env vars)

| Key | Default | Description |
|-----|---------|-------------|
| `Jwt:Issuer` | `saas-api` | Token issuer |
| `Jwt:Audience` | `saas-client` | Token audience |
| `Jwt:ExpiryMinutes` | `15` | Access token lifetime |
| `Jwt:RefreshTokenExpiryDays` | `7` | Refresh token lifetime |

### Email (Resend)

| Key | Description |
|-----|-------------|
| `Resend:ApiKey` | Resend API key for transactional email |
| `Resend:FromEmail` | Sender address (e.g. `noreply@yourdomain.com`) |

Leave `Resend:ApiKey` empty in development — email calls will no-op silently (logged at Debug level).

---

## Running the API

Once running:

- **Swagger UI:** `https://localhost:{port}/swagger`
- **Health check:** `GET /health` → `200 Healthy`

The port is assigned by `launchSettings.json` (default HTTPS: 49840, HTTP: 49841). Visual Studio shows it in the output window on startup.

---

## API Reference

All request and response bodies are JSON. Authenticated endpoints require the header:

```
Authorization: Bearer {accessToken}
```

Endpoints that operate within an organization additionally require:

```
X-Organization-Id: {orgId}
```

See [How the Org Context Header Works](#how-the-org-context-header-works) for details.

---

### Auth Endpoints

#### `POST /auth/register`

Creates a new user account.

**No auth required.**

**Request body:**
```json
{
  "email": "user@example.com",
  "password": "Password123!"
}
```

Validation: email must be valid, password minimum 8 characters. Email is normalized to lowercase.

**Response `201`:**
```json
{
  "accessToken": "eyJ...",
  "refreshToken": "abc123...",
  "expiresAt": "2026-04-15T13:03:00Z",
  "user": {
    "id": "3fa85f64-...",
    "email": "user@example.com",
    "emailVerified": false
  }
}
```

**Errors:** `409 Conflict` — email already registered.

---

#### `POST /auth/login`

Authenticates a user and returns tokens.

**No auth required.**

**Request body:**
```json
{
  "email": "user@example.com",
  "password": "Password123!"
}
```

**Response `200`:** same shape as `/auth/register`.

**Errors:** `401 Unauthorized` — wrong email or password (intentionally vague).

---

#### `POST /auth/refresh`

Exchanges a refresh token for a new access token. The old refresh token is consumed and a new one is returned (rotation).

**No auth required.**

**Request body:**
```json
{
  "refreshToken": "abc123..."
}
```

**Response `200`:** same shape as `/auth/register` with new tokens.

**Errors:** `401 Unauthorized` — token expired, already used, or invalid.

---

#### `POST /auth/logout`

Revokes the refresh token so it cannot be used again.

**Auth required.**

**Request body:**
```json
{
  "refreshToken": "abc123..."
}
```

**Response `204 No Content`.**

---

#### `POST /auth/verify-email`

Confirms the user's email address using the token sent in the verification email.

**No auth required.**

**Request body:**
```json
{
  "token": "email-verify-token"
}
```

**Response `204 No Content`.**

**Errors:** `400 Bad Request` — token invalid or expired.

---

#### `POST /auth/forgot-password`

Sends a password reset email. Always returns `202` regardless of whether the email exists (prevents user enumeration).

**No auth required.**

**Request body:**
```json
{
  "email": "user@example.com"
}
```

**Response `202 Accepted`.** No response body.

---

#### `POST /auth/reset-password`

Sets a new password using the token from the reset email.

**No auth required.**

**Request body:**
```json
{
  "token": "reset-token",
  "newPassword": "NewPassword123!"
}
```

**Response `204 No Content`.**

**Errors:** `400 Bad Request` — token invalid, expired, or new password too short.

---

### Organization Endpoints

All organization endpoints require authentication.

#### `POST /orgs`

Creates a new organization. The caller automatically becomes its **Owner**.

**Auth required.**

**Request body:**
```json
{
  "name": "Acme Corp"
}
```

A URL-friendly slug is generated from the name (e.g. `acme-corp`). If the slug already exists, a numeric suffix is appended (`acme-corp-2`).

**Response `201`:**
```json
{
  "id": "3fa85f64-...",
  "name": "Acme Corp",
  "slug": "acme-corp",
  "createdAt": "2026-04-15T12:00:00Z",
  "memberCount": 1
}
```

---

#### `GET /orgs`

Lists all organizations the current user is a member of.

**Auth required.**

**Response `200`:** array of `OrgResponse` objects (same shape as above).

---

#### `GET /orgs/{id}`

Returns a single organization. User must be a member.

**Auth required.**

**Response `200`:** single `OrgResponse`.

**Errors:** `404 Not Found` — org doesn't exist or caller is not a member.

---

#### `GET /orgs/{id}/members`

Lists all members of the organization.

**Auth required** (must be a member of the org).

**Response `200`:**
```json
[
  {
    "id": "member-guid",
    "userId": "user-guid",
    "email": "user@example.com",
    "role": "Owner",
    "joinedAt": "2026-04-15T12:00:00Z"
  }
]
```

Role values: `Owner`, `Admin`, `Member`, `Viewer`.

---

#### `POST /orgs/{id}/invites`

Sends an invitation email to a new member.

**Auth required + `members.manage` permission** (Admin or Owner role in the org, via `X-Organization-Id` header).

**Request body:**
```json
{
  "email": "newmember@example.com",
  "role": "Member"
}
```

Role must not be `Owner` — you cannot invite someone directly as Owner.

**Response `202 Accepted`:** no body (token sent by email).

**Errors:**
- `400 Bad Request` — role is `Owner`.
- `403 Forbidden` — caller does not have `members.manage` permission.

---

### Member Endpoints

#### `PATCH /members/{id}/role`

Changes a member's role within the organization.

**Auth required + `members.manage` permission** (`X-Organization-Id` header required).

`{id}` is the **membership ID** (from `GET /orgs/{id}/members`), not the user ID.

**Request body:**
```json
{
  "role": "Admin"
}
```

Role values: `Admin`, `Member`, `Viewer`. Cannot change the Owner's role or promote anyone to Owner.

**Response `204 No Content`.**

**Errors:**
- `400 Bad Request` — target is the Owner, or new role is `Owner`.
- `403 Forbidden` — caller lacks permission.
- `404 Not Found` — membership not found.

---

#### `DELETE /members/{id}`

Removes a member from the organization.

**Auth required + `members.manage` permission** (`X-Organization-Id` header required).

`{id}` is the **membership ID**.

**Response `204 No Content`.**

**Errors:**
- `400 Bad Request` — cannot remove the Owner.
- `403 Forbidden` — caller lacks permission.
- `404 Not Found` — membership not found.

---

### Invite Endpoints

#### `POST /invites/accept`

Accepts an organization invite. The authenticated user's email must match the invite's target email.

**Auth required.**

**Request body:**
```json
{
  "token": "invite-token-from-email"
}
```

**Response `204 No Content`.**

**Errors:**
- `400 Bad Request` — token invalid or expired.
- `403 Forbidden` — user's email doesn't match the invite.
- `409 Conflict` — user is already a member of the org.

---

---

### Billing Endpoints

Both billing endpoints require authentication, a valid `X-Organization-Id` header, and the `billing.manage` permission (Owner role only).

#### `POST /billing/checkout`

Creates a Stripe Checkout session for upgrading the organization's plan. Returns a Stripe-hosted URL — redirect the user there to complete payment.

**Auth required + `billing.manage` permission.**

**Request body:**
```json
{ "priceId": "price_1Abc..." }
```

Get the `priceId` from your Stripe Dashboard (Products → Pricing).

**Response `200`:**
```json
{ "url": "https://checkout.stripe.com/c/pay/..." }
```

**Errors:**
- `400 Bad Request` — `priceId` missing.
- `403 Forbidden` — caller is not the Owner.
- `404 Not Found` — org not found.

---

#### `POST /billing/portal`

Creates a Stripe Customer Portal session so the Owner can manage their subscription (cancel, update payment method, view invoices).

**Auth required + `billing.manage` permission.**

No request body.

**Response `200`:**
```json
{ "url": "https://billing.stripe.com/p/session/..." }
```

**Errors:**
- `400 Bad Request` — org has no subscription / no Stripe customer yet (must complete checkout first).
- `403 Forbidden` — caller is not the Owner.

---

### Webhook Endpoints

#### `POST /webhooks/stripe`

Receives Stripe webhook events. **This endpoint is anonymous** — security is enforced via the `Stripe-Signature` header HMAC validation (not JWT).

Configure in Stripe Dashboard → Webhooks → Add endpoint: `https://your-domain.com/webhooks/stripe`

Events handled:

| Event | Effect |
|-------|--------|
| `checkout.session.completed` | Activates subscription, links `StripeSubscriptionId` |
| `customer.subscription.updated` | Syncs status (`Active`/`Trialing`/`PastDue`/`Canceled`) and `CurrentPeriodEnd` |
| `invoice.payment_failed` | Sets subscription status to `PastDue` |

Unknown events are silently ignored (returns `200 OK`) — idempotent by design.

**Errors:** `400 Bad Request` — invalid signature.

---

## RBAC — Roles & Permissions

Each user has one role per organization. Roles map to a fixed set of permissions:

| Permission | Owner | Admin | Member | Viewer |
|-----------|:-----:|:-----:|:------:|:------:|
| `projects.read` | Yes | Yes | Yes | Yes |
| `projects.write` | Yes | Yes | Yes | No |
| `members.manage` | Yes | Yes | No | No |
| `billing.manage` | Yes | No | No | No |

**Rules:**
- There is exactly one Owner per organization. The Owner cannot be demoted or removed.
- An Admin can manage members and projects but cannot access billing.
- A Member can read and write projects but cannot manage other members.
- A Viewer has read-only access.

Permissions are checked by `PermissionAuthorizationHandler` using the role stored in `HttpContext.Items` by `OrganizationContextMiddleware`.

---

---

## Feature Gating

`IFeatureGate.IsEnabledAsync(feature, orgId)` checks whether a feature is unlocked by the organization's active plan.

Features are stored as JSON in `Plan.FeaturesJson`:

```json
// Free plan
{ "max_members": 3, "advanced_reports": false }

// Pro plan
{ "max_members": 20, "advanced_reports": true }

// Team plan
{ "max_members": 100, "advanced_reports": true }
```

**Rules:**
- Only `Active` or `Trialing` subscriptions count — `PastDue`, `Canceled`, and `Incomplete` return `false`.
- If the org has no subscription, returns `false`.
- Features not present in the JSON return `false`.

**Example usage in a controller:**
```csharp
if (!await _featureGate.IsEnabledAsync("advanced_reports", orgId))
    return StatusCode(403, new { error = "Upgrade to Pro to access advanced reports." });
```

---

## Stripe Setup

1. Create a Stripe account at [stripe.com](https://stripe.com).
2. Get your **Secret Key** from Dashboard → Developers → API Keys.
3. Create Products + Prices for Free/Pro/Team plans. Copy the Price IDs (`price_xxx`) into `Plan.StripePriceId` via a migration seed.
4. Add a webhook endpoint in Dashboard → Developers → Webhooks pointing to `/webhooks/stripe`. Copy the **Webhook Signing Secret**.
5. Set env vars or config:
   ```
   STRIPE_SECRET_KEY=sk_test_...
   STRIPE_WEBHOOK_SECRET=whsec_...
   ```
6. For local webhook testing, use the [Stripe CLI](https://stripe.com/docs/stripe-cli):
   ```bash
   stripe listen --forward-to localhost:5000/webhooks/stripe
   ```

---

## How the Org Context Header Works

For endpoints that require organization-scoped permissions, the client must send:

```
X-Organization-Id: {orgId}
```

**What happens on each request:**

1. `OrganizationContextMiddleware` reads the header.
2. It calls `IPermissionService.GetRoleAsync(userId, orgId)` to look up the user's role (with Redis/in-memory caching).
3. The resolved `OrganizationId` and `MemberRole` are stored in `HttpContext.Items`.
4. `PermissionAuthorizationHandler` reads `MemberRole` from Items and checks whether the required permission is satisfied.

If the header is missing or the user is not a member of the specified org, permission-gated endpoints return `403 Forbidden`.

---

## Token Strategy

| Token | Lifetime | Storage |
|-------|----------|---------|
| Access token (JWT) | 15 minutes | Client memory (not persisted) |
| Refresh token | 7 days | Redis (or in-memory fallback) |

**Rotation:** Every `/auth/refresh` call consumes the old refresh token and issues a new one. Reusing a consumed token returns `401`.

**Logout:** Calls `/auth/logout` with the refresh token — it is deleted from the store immediately.

**In-memory fallback:** When `REDIS_URL` is not set, `InMemoryTokenStore` is used. Tokens survive app restarts in Redis but not in-memory. The app logs a warning on startup:
```
Using InMemoryTokenStore — tokens are not shared across processes. Configure REDIS_URL for production use.
```

---

## Project Structure

```
backend/
├── backend.sln
├── nuget.config                         # Package source config
├── src/
│   ├── Domain/
│   │   ├── Entities/                    # User, Organization, OrganizationMember, Invite, Plan
│   │   └── Enums/                       # MemberRole
│   ├── Application/
│   │   ├── Auth/
│   │   │   ├── AuthService.cs           # Register, Login, Refresh, Logout, etc.
│   │   │   └── Dtos/                    # Request/response records
│   │   ├── Organizations/
│   │   │   ├── OrganizationService.cs   # Create, invite, accept, change role, remove
│   │   │   └── Dtos/
│   │   └── Common/
│   │       ├── Constants/Permissions.cs # Role → permission mapping
│   │       ├── Exceptions/AppException.cs
│   │       ├── Interfaces/              # IAppDbContext, ITokenService, IEmailService, etc.
│   │       └── Settings/JwtSettings.cs
│   ├── Infrastructure/
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/          # EF Core entity configurations + plan seeds
│   │   │   └── Migrations/              # Auto-generated migration files
│   │   └── Services/
│   │       ├── BCryptPasswordHasher.cs
│   │       ├── JwtTokenService.cs
│   │       ├── RedisTokenStore.cs
│   │       ├── InMemoryTokenStore.cs    # Dev fallback (no Redis needed)
│   │       ├── PermissionService.cs
│   │       └── ResendEmailService.cs
│   └── Api/
│       ├── Program.cs
│       ├── Controllers/                 # AuthController, OrgsController, MembersController, InvitesController
│       ├── Middleware/
│       │   ├── GlobalExceptionMiddleware.cs
│       │   ├── RequestContextMiddleware.cs
│       │   └── OrganizationContextMiddleware.cs
│       ├── Policies/
│       │   ├── PermissionRequirement.cs
│       │   └── PermissionAuthorizationHandler.cs
│       ├── Extensions/HttpContextExtensions.cs
│       └── appsettings*.json
└── tests/
    ├── Application.UnitTests/           # xUnit, FluentAssertions, NSubstitute, in-memory EF
    │   ├── Auth/AuthServiceTests.cs
    │   ├── Organizations/OrganizationServiceTests.cs
    │   ├── Rbac/PermissionServiceTests.cs
    │   └── TestInfrastructure/TestDbContext.cs
    └── Api.IntegrationTests/            # Full HTTP tests — requires Docker (Testcontainers)
        ├── Infrastructure/IntegrationTestBase.cs
        └── Organizations/
            ├── OrgsEndpointsTests.cs
            └── MembersEndpointsTests.cs
```

---

## Running Tests

### Unit tests (no external dependencies)

```bash
dotnet test tests/Application.UnitTests
```

Or in Visual Studio: Test → Run All Tests.

These use an in-memory EF Core database and NSubstitute mocks. They work without PostgreSQL or Redis.

### Integration tests (requires Docker)

```bash
dotnet test tests/Api.IntegrationTests
```

These use Testcontainers to spin up real PostgreSQL and Redis containers for each test class. **Docker must be running.**

---

## Database Migrations

### Create a new migration

Run in Package Manager Console (set Api as startup project):

```powershell
Add-Migration <MigrationName> -Project Infrastructure -StartupProject Api
```

### Apply migrations manually

```powershell
Update-Database -Project Infrastructure -StartupProject Api
```

### Auto-apply on startup

In Development, `Program.cs` calls `db.Database.MigrateAsync()` on startup — no manual step needed after creating the migration file.

### Undo the last migration (before applying)

```powershell
Remove-Migration -Project Infrastructure -StartupProject Api
```
