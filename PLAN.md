# SaaS RBAC Boilerplate — Phase-wise Implementation Plan

## Overview

Building a **production-ready multi-tenant SaaS boilerplate** using:
- **Frontend**: Next.js 15 (App Router), TypeScript, TailwindCSS, shadcn/ui, Zustand, React Query
- **Backend**: ASP.NET Core 8, EF Core, PostgreSQL, Redis, JWT, Stripe.net
- **Infra**: Docker, Docker Compose, GitHub Actions, Nginx, Seq, Resend

---

## Phase 1 — Foundation & Infrastructure ✅ (Setup Complete)

**Goal**: Runnable skeleton with all services wired up via Docker Compose.

### Deliverables
- [x] Monorepo directory structure
- [x] `docker-compose.yml` — postgres, redis, seq, backend, frontend
- [x] `docker-compose.prod.yml` — production compose with Nginx
- [x] `.env.example` — all required environment variables documented
- [x] `Makefile` — `make dev`, `make migrate`, `make seed`, `make test`
- [x] `.github/workflows/ci.yml` — PR: lint + test + build images
- [x] `.github/workflows/deploy.yml` — Merge to main: push GHCR + SSH deploy

### Tasks remaining in Phase 1
- [ ] `docker/nginx/nginx.conf` — reverse proxy config
- [ ] `backend/Dockerfile` (dev + prod multi-stage)
- [ ] `frontend/Dockerfile` (dev + prod multi-stage)
- [ ] `.gitignore`

---

## Phase 2 — Backend: Database & Authentication

**Goal**: Working auth system with JWT + Redis refresh tokens.

### 2.1 — Project Setup
- [ ] Create ASP.NET Core 8 Web API project (`backend/src/Api`)
- [ ] Create class library projects:
  - `backend/src/Domain` — entities, enums, interfaces
  - `backend/src/Application` — services, DTOs, validators
  - `backend/src/Infrastructure` — EF Core, Redis, external services
- [ ] Add project references and solution file
- [ ] Configure Serilog + Seq sink
- [ ] Configure Swagger/OpenAPI

### 2.2 — Database Schema (EF Core + PostgreSQL)
Entities to create:
- [ ] `User` (Id, Email, PasswordHash, EmailVerified, CreatedAt)
- [ ] `Organization` (Id, Name, Slug, OwnerId, CreatedAt)
- [ ] `OrganizationMember` (Id, UserId, OrganizationId, Role, JoinedAt)
- [ ] `Invite` (Id, OrganizationId, Email, Role, Token, Status, ExpiresAt)
- [ ] `Plan` (Id, Name, StripePriceId, FeaturesJson)
- [ ] `Subscription` (Id, OrganizationId, StripeCustomerId, StripeSubscriptionId, PlanId, Status, CurrentPeriodEnd)
- [ ] `AuditEvent` (Id, OrganizationId, ActorUserId, Action, MetadataJson, CreatedAt)
- [ ] `AppDbContext` with DbSets and configurations
- [ ] Initial EF Core migration

### 2.3 — Authentication Endpoints
- [ ] `POST /auth/register` — hash password with BCrypt, create user, send verify email
- [ ] `POST /auth/login` — validate credentials, issue JWT (15 min) + refresh token in Redis (7 days)
- [ ] `POST /auth/refresh` — validate + rotate refresh token, issue new JWT
- [ ] `POST /auth/logout` — invalidate refresh token in Redis
- [ ] `POST /auth/verify-email` — verify email token, set `EmailVerified = true`
- [ ] `POST /auth/forgot-password` — send reset link via Resend
- [ ] JWT middleware configuration
- [ ] Redis token store service

### 2.4 — Logging Middleware
- [ ] Request logging middleware: RequestId, UserId, OrganizationId, Timestamp
- [ ] Global exception handler middleware
- [ ] Structured log enrichers

---

## Phase 3 — Backend: RBAC & Organization System

**Goal**: Multi-tenant organization management with policy-based authorization.

### 3.1 — RBAC Engine
Permission definitions:
```
projects.read  | projects.write | members.manage | billing.manage
```

Role-to-permission mapping:
```
Owner  → all permissions
Admin  → projects.*, members.manage
Member → projects.read, projects.write
Viewer → projects.read
```

- [ ] `IPermissionService` — resolves permissions for a user in an org
- [ ] ASP.NET Core authorization policies for each permission
- [ ] `OrganizationContextMiddleware` — reads `X-Organization-Id` header, resolves membership
- [ ] `[Authorize(Policy="projects.write")]` attribute usage pattern
- [ ] Permission cache in Redis (per user+org, TTL 5 min)

### 3.2 — Organization Endpoints
- [ ] `POST /orgs` — create org, add creator as Owner
- [ ] `GET /orgs` — list orgs for current user
- [ ] `GET /orgs/{id}` — get org details (requires membership)
- [ ] `PATCH /orgs/{id}` — update org name (Admin+)

### 3.3 — Member & Invite Endpoints
- [ ] `POST /orgs/{id}/invites` — create invite with signed token (HMAC), send email via Resend (48h expiry)
- [ ] `POST /invites/accept` — validate token, create OrganizationMember
- [ ] `GET /orgs/{id}/members` — list members (Member+)
- [ ] `PATCH /members/{id}/role` — change role (Admin+, cannot demote Owner)
- [ ] `DELETE /members/{id}` — remove member (Admin+)

### 3.4 — Audit Logging
- [ ] `IAuditService` — records actions to `AuditEvents` table
- [ ] Hook into org/member/invite operations

---

## Phase 4 — Backend: Billing & Feature Gating

**Goal**: Stripe subscriptions wired end-to-end with feature gates.

### 4.1 — Stripe Integration
- [ ] Configure `Stripe.net` SDK with `STRIPE_SECRET_KEY`
- [ ] Seed `Plans` table (Free, Pro, Team) with Stripe Price IDs
- [ ] `POST /billing/checkout` — create Stripe Checkout Session, return URL
- [ ] `POST /billing/portal` — create Stripe Customer Portal session, return URL

### 4.2 — Webhook Handler
- [ ] `POST /webhooks/stripe` — validate Stripe signature (`STRIPE_WEBHOOK_SECRET`)
- [ ] Handle `checkout.session.completed` → create/update `Subscription`
- [ ] Handle `customer.subscription.updated` → sync `Subscription.Status`
- [ ] Handle `invoice.payment_failed` → mark subscription past-due, send email
- [ ] Idempotency: skip if event already processed (store processed event IDs in Redis)

### 4.3 — Feature Gate Service
- [ ] `IFeatureGate` interface
- [ ] `FeatureGateService` — reads plan `FeaturesJson`, checks org subscription
- [ ] Usage: `FeatureGate.IsEnabled("advanced_reports", organizationId)`
- [ ] Returns `403` from API when feature not enabled
- [ ] Expose feature flags in `/orgs/{id}/features` endpoint for frontend

---

## Phase 5 — Frontend

**Goal**: Full Next.js 15 App Router frontend with auth, dashboard, billing UI.

### 5.1 — Project Bootstrap
- [ ] `create-next-app` with TypeScript + TailwindCSS + App Router
- [ ] Install shadcn/ui, Zustand, React Query (`@tanstack/react-query`)
- [ ] Configure API client (axios/fetch wrapper with token refresh interceptor)
- [ ] Configure Zustand auth store (user, tokens, org context)

### 5.2 — Auth Pages
- [ ] `/register` — registration form → `POST /auth/register`
- [ ] `/login` — login form → `POST /auth/login`, store tokens
- [ ] `/verify-email` — handle verification link
- [ ] `/forgot-password` — request reset form
- [ ] Middleware: redirect unauthenticated users to `/login`

### 5.3 — Dashboard Layout
- [ ] Root layout with:
  - `<Sidebar>` — org switcher, nav links
  - `<Topbar>` — breadcrumb, user menu
  - `<main>` — page content
- [ ] `/dashboard` — overview/home page

### 5.4 — Settings Pages
- [ ] `/settings` — profile settings (name, email, password change)
- [ ] `/settings/members` — member list, invite form, role change, remove
- [ ] `/settings/billing` — current plan, upgrade button, billing portal link
  - Show upgrade prompt if feature not available

### 5.5 — RBAC on Frontend
- [ ] `usePermission(permission)` hook — reads org membership role
- [ ] Hide/disable UI elements based on permission
- [ ] Handle `403` responses with feature upgrade prompt

### 5.6 — Organization Switcher
- [ ] Zustand store: `currentOrgId`
- [ ] Switcher component in sidebar
- [ ] `X-Organization-Id` header sent on all API requests

---

## Phase 6 — Testing

**Goal**: 70%+ coverage on critical paths.

### 6.1 — Backend Integration Tests (xUnit + Testcontainers)
- [ ] Test infrastructure: `WebApplicationFactory` + Testcontainers (postgres, redis)
- [ ] Auth flow tests:
  - Register → Login → Refresh → Logout
  - Email verification
  - Invalid credentials
- [ ] RBAC tests:
  - Owner can do everything
  - Member blocked from `members.manage`
  - Viewer blocked from `projects.write`
  - Cross-org access blocked
- [ ] Stripe webhook tests:
  - Valid signature → subscription updated
  - Invalid signature → 400
  - Duplicate event → idempotent (no duplicate update)

### 6.2 — Frontend Tests
- [ ] Unit tests for permission hooks
- [ ] Integration tests for auth forms (React Testing Library)

---

## Phase 7 — Production & Polish

**Goal**: Production-ready deployment with Nginx, CI/CD, and docs.

### 7.1 — Nginx Config
- [ ] `docker/nginx/nginx.conf` — reverse proxy:
  - `/api/*` → backend:5000
  - `/*` → frontend:3000
  - SSL termination (certbot / self-signed for dev)

### 7.2 — Production Hardening
- [ ] Rate limiting on `/auth/*` endpoints
- [ ] CORS configuration (restrict to `APP_URL`)
- [ ] Security headers middleware (HSTS, X-Frame-Options, etc.)
- [ ] Health check endpoints (`GET /health`)

### 7.3 — Documentation
- [ ] `docs/architecture.md` — system design, data flow diagrams
- [ ] `docs/setup.md` — 5-step onboarding guide
- [ ] `docs/api.md` — endpoint reference
- [ ] `docs/rbac.md` — permission model explained

### 7.4 — Demo Instance
- [ ] Configure production server
- [ ] Seed demo data (users, orgs, plans)
- [ ] Live URL documented in README

---

## Implementation Order & Dependencies

```
Phase 1 (Infra)
    └─▶ Phase 2 (Backend Auth)
            └─▶ Phase 3 (RBAC + Orgs)
                    ├─▶ Phase 4 (Billing)
                    └─▶ Phase 5 (Frontend)
                            └─▶ Phase 6 (Testing)
                                    └─▶ Phase 7 (Prod)
```

Phases 4 and 5 can run in parallel once Phase 3 is complete.

---

## Key Technical Decisions

| Decision | Choice | Reason |
|---|---|---|
| Auth tokens | JWT (15 min) + Redis refresh (7 days, rotating) | Stateless access, revocable refresh |
| Password hashing | BCrypt | Industry standard, adaptive cost |
| RBAC | ASP.NET Core authorization policies | Clean, testable, declarative |
| Org resolution | `X-Organization-Id` header + middleware | Explicit, multi-org safe |
| Invite tokens | HMAC-signed | Tamper-proof, no DB lookup for validation |
| Email | Resend API | Modern, developer-friendly |
| Logging | Serilog + Seq | Structured logs, excellent search |
| Testing | xUnit + Testcontainers | Real DB/Redis in tests, no mocks |

---

## Quick Start (after all phases complete)

```bash
git clone <repo>
cp .env.example .env        # step 1: copy env
# edit .env with your keys  # step 2: add secrets
make dev                    # step 3: start everything
make migrate                # step 4: run migrations
make seed                   # step 5: seed data
# open http://localhost:3000
```
