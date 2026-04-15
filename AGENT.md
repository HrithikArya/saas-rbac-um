# AGENT.md

## Project: SaaS Boilerplate (Next.js + ASP.NET Core)

This document defines how the development agent should implement the SaaS boilerplate system.

The goal is to build a **production-ready multi-tenant SaaS foundation** with:

* Authentication
* Organizations (multi-tenancy)
* RBAC permission engine
* Stripe subscription billing
* Dockerized infrastructure
* CI/CD pipeline
* Developer-friendly local setup

The repository follows a **monorepo structure**.

---

# 1. Repository Structure

```
root/
 ├ frontend/        # Next.js 15 App Router
 ├ backend/         # ASP.NET Core 8 API
 ├ docker/
 ├ docs/
 ├ docker-compose.yml
 ├ docker-compose.prod.yml
 ├ .env.example
 └ AGENT.md
```

Frontend responsibilities:

* UI
* authentication flows
* billing UI
* team management UI

Backend responsibilities:

* authentication API
* RBAC authorization
* billing logic
* webhook handling
* database operations

---

# 2. Tech Stack

## Frontend

* Next.js 15 (App Router)
* TypeScript
* TailwindCSS
* shadcn/ui
* Zustand (global state)
* React Query (server state)

## Backend

* ASP.NET Core 8
* Entity Framework Core
* PostgreSQL
* Redis
* JWT authentication
* Stripe.net SDK

## Infrastructure

* Docker
* Docker Compose
* GitHub Actions
* Nginx
* Seq logging
* Resend email API

---

# 3. Core System Architecture

The system is **multi-tenant**.

Every piece of business data must be scoped by:

```
OrganizationId
```

A user can belong to **multiple organizations**.

Authorization requires:

```
User
→ Membership
→ Role
→ Permission
```

---

# 4. Database Schema

Core entities:

### Users

```
Id
Email
PasswordHash
EmailVerified
CreatedAt
```

### Organizations

```
Id
Name
Slug
OwnerId
CreatedAt
```

### OrganizationMembers

```
Id
UserId
OrganizationId
Role
JoinedAt
```

Roles:

```
Owner
Admin
Member
Viewer
```

### Invites

```
Id
OrganizationId
Email
Role
Token
Status
ExpiresAt
```

### Plans

```
Id
Name
StripePriceId
FeaturesJson
```

### Subscriptions

```
Id
OrganizationId
StripeCustomerId
StripeSubscriptionId
PlanId
Status
CurrentPeriodEnd
```

### AuditEvents

```
Id
OrganizationId
ActorUserId
Action
MetadataJson
CreatedAt
```

---

# 5. Authentication System

Authentication uses:

```
Access Token (JWT)
Refresh Token (stored in Redis)
```

Endpoints:

```
POST /auth/register
POST /auth/login
POST /auth/refresh
POST /auth/logout
POST /auth/verify-email
POST /auth/forgot-password
```

Rules:

* Access tokens expire in **15 minutes**
* Refresh tokens expire in **7 days**
* Refresh tokens rotate on each refresh

Passwords must be hashed using:

```
BCrypt
```

---

# 6. RBAC Authorization Engine

Authorization is **policy-based**.

Example permissions:

```
projects.read
projects.write
members.manage
billing.manage
```

Each role maps to a permission set.

Example:

Owner

```
all permissions
```

Admin

```
projects.*
members.manage
```

Member

```
projects.read
projects.write
```

Viewer

```
projects.read
```

Implementation requirements:

* Middleware resolves current organization
* Permission checks must use ASP.NET policies
* Every protected endpoint must validate permission

Example:

```
[Authorize(Policy="projects.write")]
```

---

# 7. Organization System

Users can:

* create organizations
* invite members
* change roles
* remove members

Endpoints:

```
POST /orgs
GET /orgs
POST /orgs/{id}/invites
POST /invites/accept
DELETE /members/{id}
PATCH /members/{id}/role
```

Invites must use **signed tokens**.

Expiration: **48 hours**

Emails sent using **Resend**.

---

# 8. Stripe Billing Integration

Plans:

```
Free
Pro
Team
```

Stripe configuration stored in environment variables.

Backend endpoints:

```
POST /billing/checkout
POST /billing/portal
POST /webhooks/stripe
```

Webhook events to handle:

```
checkout.session.completed
customer.subscription.updated
invoice.payment_failed
```

Webhook rules:

* Must validate Stripe signature
* Must be idempotent
* Must update local subscription state

---

# 9. Feature Gating

A service named:

```
IFeatureGate
```

must determine whether a feature is available.

Example usage:

```
FeatureGate.IsEnabled("advanced_reports", organizationId)
```

If a feature is not enabled:

* API returns `403`
* UI shows upgrade prompt

---

# 10. Frontend Pages

Required routes:

```
/login
/register
/dashboard
/settings
/settings/members
/settings/billing
```

Dashboard layout:

```
Sidebar
Topbar
Main content
```

Protected routes must redirect to `/login` if unauthenticated.

---

# 11. Docker Setup

Local development must run using:

```
docker-compose up
```

Services:

```
frontend
backend
postgres
redis
seq
```

Ports:

```
Frontend → 3000
Backend → 5000
Postgres → 5432
Redis → 6379
Seq → 5341
```

---

# 12. CI/CD Pipeline

GitHub Actions pipeline must:

### On Pull Request

* run lint
* run tests
* build docker images

### On Merge to Main

* build images
* push to GHCR
* deploy to server

---

# 13. Logging

Backend logging must use:

```
Serilog
```

Logs must include:

```
RequestId
UserId
OrganizationId
Timestamp
```

All logs should be sent to **Seq**.

---

# 14. Testing

Backend tests must use:

```
xUnit
Testcontainers
```

Test coverage targets:

```
Auth flows
RBAC permission checks
Stripe webhook handling
```

Target coverage:

```
70%+
```

---

# 15. Environment Variables

The repository must include:

```
.env.example
```

Variables include:

```
DATABASE_URL
REDIS_URL
JWT_SECRET
STRIPE_SECRET_KEY
STRIPE_WEBHOOK_SECRET
RESEND_API_KEY
NEXT_PUBLIC_API_URL
```

---

# 16. Developer Experience

Commands that must work:

```
make dev
make migrate
make seed
make test
```

Setup must require **no more than 5 steps**.

---

# 17. Definition of Done

The project is complete when:

* Users can register and login
* Organizations can be created
* Members can be invited
* RBAC permissions are enforced
* Stripe subscriptions work
* Billing UI works
* Docker setup runs locally
* CI/CD deploys automatically
* A live demo instance exists
* Documentation site explains the architecture

---

# End of AGENT.md
