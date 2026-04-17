# SaaS RBAC Boilerplate — Implementation Plan

## Overview

Building a **production-ready multi-tenant SaaS boilerplate** using:
- **Frontend**: Next.js 15 (App Router), TypeScript, TailwindCSS, shadcn/ui, Zustand, React Query
- **Backend**: ASP.NET Core 8, EF Core, PostgreSQL, Redis, JWT, Stripe.net / Razorpay
- **Infra**: Docker, Docker Compose, GitHub Actions, Nginx, Seq, Resend

---

## Phase 1 — Foundation & Infrastructure ✅

**Goal**: Monorepo skeleton, Docker Compose, CI/CD pipeline.

- [x] Monorepo directory structure
- [x] `docker-compose.yml` — postgres, redis, seq, backend, frontend
- [x] `docker-compose.prod.yml` — production compose with Nginx
- [x] `.env.example` — all required environment variables
- [x] `Makefile` — `make dev`, `make migrate`, `make test`
- [x] `.github/workflows/ci.yml` — PR: lint + test + build
- [x] `.github/workflows/deploy.yml` — merge to main: push GHCR + deploy
- [x] `backend/Dockerfile` (multi-stage)
- [x] `frontend/Dockerfile` (multi-stage)
- [x] `.gitignore` — excludes `bin/`, `obj/`, `node_modules/`, `.next/`

---

## Phase 2 — Backend: Database & Auth ✅

**Goal**: Working auth system with JWT + Redis/InMemory refresh tokens.

- [x] ASP.NET Core 8 clean architecture (Domain / Application / Infrastructure / Api)
- [x] EF Core 8 + Npgsql (PostgreSQL), code-first migrations
- [x] All entities: User, Organization, OrganizationMember, Invite, Plan, Subscription, AuditEvent
- [x] BCrypt password hashing via `IPasswordHasher` interface
- [x] JWT access tokens (15 min) + rotating refresh tokens (7 days)
- [x] Redis token store with InMemory fallback (no Docker/Redis required for dev)
- [x] Auth endpoints: register, login, refresh, logout, verify-email, forgot-password, reset-password
- [x] Serilog + Seq structured logging with RequestId/UserId/OrganizationId enrichment
- [x] Swagger/OpenAPI with Bearer auth
- [x] Global exception middleware, request context middleware
- [x] Auto-migrate on startup in Development

---

## Phase 3 — Backend: RBAC & Organizations ✅

**Goal**: Multi-tenant org management with policy-based authorization.

- [x] `Permissions.ForRole()` — Owner→all, Admin→projects.*+members.manage, Member→projects.*, Viewer→read
- [x] ASP.NET Core authorization policies for each permission string
- [x] `OrganizationContextMiddleware` — reads `X-Organization-Id`, resolves role into `HttpContext.Items`
- [x] `PermissionAuthorizationHandler` — checks role-permission mapping
- [x] Organization endpoints: POST/GET /orgs, GET /orgs/{id}, GET /orgs/{id}/members
- [x] Invite endpoints: POST /orgs/{id}/invites, POST /invites/accept
- [x] Member endpoints: PATCH /members/{id}/role, DELETE /members/{id}
- [x] `IPermissionService` with Redis/InMemory 5-min cache
- [x] `IAuditService` — writes to AuditEvents, never throws
- [x] Unit tests: PermissionsTests, PermissionServiceTests, OrganizationServiceTests
- [x] Integration tests: OrgsEndpointsTests, MembersEndpointsTests

---

## Phase 4 — Backend: Billing & Feature Gating ✅

**Goal**: Payment integration wired end-to-end with feature gates.

- [x] `IBillingService` interface (Application layer — payment-provider agnostic)
- [x] `IStripeGateway` thin wrapper for testability (Infrastructure layer)
- [x] `StripeBillingService` — checkout session, customer portal, webhook processing
- [x] `MockBillingService` — dev fallback when no payment keys configured (India / no Stripe)
- [x] Conditional DI: uses real Stripe if `STRIPE_SECRET_KEY` set, otherwise mock
- [x] Webhook handler: `checkout.session.completed`, `customer.subscription.updated`, `invoice.payment_failed`
- [x] `IFeatureGate` + `FeatureGateService` — reads `Plan.FeaturesJson`, checks active subscription
- [x] Billing endpoints: POST /billing/checkout, POST /billing/portal
- [x] Webhook endpoint: POST /webhooks/stripe (raw body + signature validation)
- [x] Unit tests: BillingServiceTests (8 tests), FeatureGateTests (6 tests)
- [x] Plans seeded: Free `{max_members:3, advanced_reports:false}`, Pro `{20, true}`, Team `{100, true}`

> **India note**: Stripe is invite-only in India. The `MockBillingService` lets you build the full
> billing UI without real keys. For production, swap `StripeGatewayAdapter` with `RazorpayGatewayAdapter`
> — the `IBillingService` interface stays unchanged.

---

## Phase 5 — Frontend ✅

**Goal**: Full Next.js 15 App Router frontend — auth flows, dashboard, settings, billing UI.

### 5.1 — Project Bootstrap ✅
- [x] Next.js 15 + TypeScript + TailwindCSS + App Router
- [x] shadcn/ui component library (Button, Input, Card, Dialog, Toast, Select, Badge, etc.)
- [x] Zustand 5 state management
- [x] TanStack React Query 5 for server state
- [x] Axios API client with token refresh interceptor + org context header

### 5.2 — Auth Pages ✅
- [x] `/login` — login form → POST /auth/login, store tokens
- [x] `/register` — registration form → POST /auth/register
- [x] `/forgot-password` — request reset email
- [x] `/verify-email` — confirm token from email link
- [x] Client-side auth guard in dashboard layout

### 5.3 — Dashboard Layout ✅
- [x] Root layout with React Query + Zustand providers
- [x] `<Sidebar>` — org switcher, nav links (Dashboard, Settings)
- [x] `<Topbar>` — breadcrumb, user menu with logout
- [x] `<OrgSwitcher>` — lists orgs, switches `currentOrgId`
- [x] `/dashboard` — overview page

### 5.4 — Settings Pages ✅
- [x] `/settings` — profile placeholder
- [x] `/settings/members` — member table, invite dialog (Admin+), role change, remove
- [x] `/settings/billing` — current plan card, upgrade button (Owner only), billing portal

### 5.5 — RBAC on Frontend ✅
- [x] `usePermission(permission)` hook — derives from current org member role
- [x] `useCurrentMemberRole()` hook
- [x] UI elements hidden/disabled by permission
- [x] `X-Organization-Id` header automatically injected by API client

### 5.6 — Polish ✅
- [x] `/reset-password` page — token from email link, confirm new password
- [x] Org creation dialog — `<NewOrgDialog>` in sidebar OrgSwitcher
- [x] Toast notifications — `useToast` + `<Toaster>` (Radix) wired into all actions
- [x] `DialogFooter` export added to dialog.tsx

---

## Phase 6 — Testing 🔄 IN PROGRESS

**Goal**: 70%+ coverage on critical paths.

### 6.1 — Backend Integration Tests (requires Docker for Testcontainers) ✅
- [x] Auth flow: Register → Login → Refresh → Logout + token rotation
- [x] RBAC: Owner/Admin/Member/Viewer permission boundaries (orgs + members tests)
- [x] Billing: checkout (mock), portal (mock), subscription info, org rename
- [ ] Webhook: valid signature, invalid signature (needs Stripe test keys)

### 6.2 — Frontend Tests 🔄 IN PROGRESS
- [x] Vitest + React Testing Library setup (`vitest.config.ts`, `src/test/setup.ts`)
- [x] `usePermission` hook unit tests — all 4 roles, 6 test cases
- [ ] Auth form integration tests (login/register flow with mocked API)

---

## Phase 7 — Production & Polish

**Goal**: Production-ready deployment.

### 7.1 — Nginx
- [ ] `docker/nginx/nginx.conf` — `/api/*` → backend:5000, `/*` → frontend:3000, SSL

### 7.2 — Hardening
- [ ] Rate limiting on `/auth/*`
- [ ] Security headers (HSTS, X-Frame-Options, CSP)
- [ ] Stripe/Razorpay production keys + webhook endpoint

### 7.3 — Docs
- [ ] `docs/architecture.md`
- [ ] `docs/setup.md` — 5-step onboarding

---

## Dependency Graph

```
Phase 1 (Infra)
    └─▶ Phase 2 (Backend Auth)
            └─▶ Phase 3 (RBAC + Orgs)
                    ├─▶ Phase 4 (Billing)      ✅ done
                    └─▶ Phase 5 (Frontend)     🔄 in progress
                            └─▶ Phase 6 (Tests)
                                    └─▶ Phase 7 (Prod)
```

---

## Key Technical Decisions

| Decision | Choice | Reason |
|---|---|---|
| Auth tokens | JWT (15 min) + Redis refresh (7 days, rotating) | Stateless access, revocable refresh |
| Password hashing | BCrypt | Industry standard, adaptive cost |
| RBAC | ASP.NET Core authorization policies | Clean, testable, declarative |
| Org resolution | `X-Organization-Id` header + middleware | Explicit, multi-org safe |
| Billing (global) | Stripe.net + `IBillingService` abstraction | Swappable provider |
| Billing (India) | MockBillingService → Razorpay when ready | Stripe invite-only in India |
| Token store | Redis → InMemory fallback | Dev without Redis/Docker |
| Email | Resend API | Modern, developer-friendly |
| Logging | Serilog + Seq | Structured logs, excellent search |
| Frontend state | Zustand (auth/org) + React Query (server) | Correct separation of concerns |
| UI library | shadcn/ui + Radix + Tailwind | Accessible, unstyled primitives |
