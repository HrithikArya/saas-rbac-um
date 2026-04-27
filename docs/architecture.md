# Architecture

## Overview

Multi-tenant SaaS boilerplate — JWT auth, RBAC, billing, and feature gating.

```
┌──────────────────┐     ┌──────────────────┐     ┌────────────┐
│   Next.js 15     │────▶│  ASP.NET Core 8  │────▶│ PostgreSQL │
│   (App Router)   │     │      API         │     │  (EF Core) │
└──────────────────┘     └────────┬─────────┘     └────────────┘
                                  │
                     ┌────────────┴────────────┐
                     │                         │
               ┌─────▼──────┐          ┌───────▼──────┐
               │   Redis    │          │    Resend    │
               │  (tokens,  │          │   (email)    │
               │permissions)│          └──────────────┘
               └────────────┘
```

---

## Backend layers (Clean Architecture)

| Layer | Project | Responsibility |
|---|---|---|
| Domain | `Domain` | Entities only — no dependencies |
| Application | `Application` | Business logic, service interfaces |
| Infrastructure | `Infrastructure` | EF Core, Redis, email, billing adapters |
| API | `Api` | Controllers, middleware, DI root |

---

## Auth flow

```
POST /auth/register  →  create user, send verification email
POST /auth/login     →  JWT (15 min) + refresh token (7 days, stored Redis/InMemory)
POST /auth/refresh   →  rotate refresh token, issue new JWT
POST /auth/logout    →  revoke refresh token
```

---

## Multi-tenancy

Every authenticated request carries an `X-Organization-Id` header.
`OrganizationContextMiddleware` resolves the user's role in that org and injects it
into `HttpContext.Items`. All RBAC checks read from there — no per-request DB calls
after the initial resolution.

---

## RBAC

| Role | Permissions |
|---|---|
| Owner | All permissions |
| Admin | `projects.*`, `members.manage` |
| Member | `projects.*` |
| Viewer | `*.read` only |

Permission checks use ASP.NET Core authorization policies (`[Authorize(Policy = "projects.read")]`).
`PermissionService` caches resolved permissions in Redis/InMemory for 5 minutes.

---

## Billing

`IBillingService` interface — two implementations, selected at startup:

| Implementation | When used |
|---|---|
| `StripeBillingService` | `STRIPE_SECRET_KEY` env var is set |
| `MockBillingService` | No key — simulates checkout + portal flow |

`IFeatureGate` reads `Plan.FeaturesJson` (stored per plan in DB) to gate features at runtime.

Plans seeded: **Free** (3 members), **Pro** (20 members, advanced reports), **Team** (100 members).

---

## Token store

`ITokenStore` — two implementations, selected at startup:

| Implementation | When used |
|---|---|
| `RedisTokenStore` | `REDIS_URL` env var or `ConnectionStrings:Redis` set |
| `InMemoryTokenStore` | Neither set (local dev default) |

---

## Request pipeline order

```
Serilog request logging
Security headers (X-Frame-Options, CSP, etc.)
Global exception handler
Request context (RequestId, UserId enrichment)
CORS
Rate limiter  ←  10 req/min on /auth/*, 300 req/min elsewhere
Authentication (JWT)
OrganizationContext middleware
Authorization (RBAC policies)
Controllers
```

---

## Port map

| Service | Local (no Docker) | Docker dev | Docker prod (Nginx) |
|---|---|---|---|
| Backend API | 5000 | 5000 | 80/443 via `/api/` |
| Frontend | 3300 | 3000 | 80/443 via `/` |
| PostgreSQL | 5432 (local install) | 5432 | internal only |
| Redis | — (InMemory) | 6379 | internal only |
| Seq | — (console logs) | 5341 | internal only |
