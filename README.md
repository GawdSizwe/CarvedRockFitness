# KasiMeetups — CarvedRock Fitness Platform

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![C#](https://img.shields.io/badge/C%23-13.0-239120?logo=csharp)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![EF Core](https://img.shields.io/badge/EF_Core-9.0-512BD4?logo=dotnet)](https://docs.microsoft.com/ef/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Latest-4169E1?logo=postgresql)](https://www.postgresql.org)
[![OpenIddict](https://img.shields.io/badge/OpenIddict-5.4-blueviolet)](https://github.com/openiddict/openiddict-core)
[![Fly.io](https://img.shields.io/badge/Fly.io-LIVE-8B5CF6?logo=fly.io)](https://fly.io)

> **Enterprise-grade community platform** built on ASP.NET Core 9 — microservices architecture, OAuth2/OpenID Connect identity, role-based access control, per-endpoint rate limiting, and split health checks. Live in Johannesburg (JNB).
>
> _Clean Architecture · OAuth2/OpenID Connect · Role-Based Access · Live on Fly.io_

[Live Demo](#live-demo) · [Architecture](#architecture) · [Quick Start](#quick-start) · [Features](#features) · [Deployment](#deployment) · [Observability](#observability)

---

## Author

| | |
|---|---|
| **Name** | Sizwe Mazibuko |
| **Role** | Software Engineer / Tech Lead |
| **GitHub** | [github.com/GawdSizwe](https://github.com/GawdSizwe) |
| **Email** | smazibuko01@gmail.com |

---

## Live Demo

| Service | URL | Status |
|---------|-----|--------|
| Web App | https://gawdsizwe-web.fly.dev | Live |
| REST API | https://gawdsizwe-api.fly.dev | Live |
| Auth Server | https://gawdsizwe-auth.fly.dev | Live |
| API Health | https://gawdsizwe-api.fly.dev/healthz | Live |

### Demo Credentials

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@kasimeetups.co.za | Admin1234! |
| Member | member@kasimeetups.co.za | Member1234! |

---

## Architecture

Four independently deployed microservices — each with a single responsibility, zero shared state, all communicating over HTTPS with signed JWT tokens.

```
+------------------------------------------------------------------+
|                         Fly.io Cloud (JNB)                       |
|                                                                   |
|  +------------------+   OIDC/OAuth2   +----------------------+  |
|  |   gawdsizwe-web  | --------------> |  gawdsizwe-auth      |  |
|  |                  |                 |                      |  |
|  |  ASP.NET Core 9  |                 |  OpenIddict 5.4      |  |
|  |  MVC + Razor     |                 |  OAuth2 / OIDC       |  |
|  |  256MB · :8080   |                 |  512MB · :8080       |  |
|  +--------+---------+                 +----------------------+  |
|           |                                                       |
|           |  Bearer JWT                                           |
|           v                                                       |
|  +------------------+                 +----------------------+  |
|  |   gawdsizwe-api  | --------------> |      gym-db          |  |
|  |                  |                 |                      |  |
|  |  ASP.NET Core 9  |                 |  PostgreSQL          |  |
|  |  REST Web API    |                 |  Fly Managed DB      |  |
|  |  512MB · :8080   |                 |  1GB Storage         |  |
|  +------------------+                 +----------------------+  |
|                                                                   |
+------------------------------------------------------------------+
```

### Why This Architecture?

| Principle | Implementation |
|-----------|---------------|
| **Single Responsibility** | Each service does one job — present, serve, authenticate, or store |
| **Failure Isolation** | Auth outage does not affect API data serving |
| **Independent Deployment** | Redeploy any service without touching others |
| **Zero Shared State** | No shared memory, no shared session — only JWT claims |
| **Database Ownership** | Only the API holds the DB connection string |

### Internal N-Tier Structure (per service)

```
Controller  (HTTP in/out — no business logic)
    |
Service Layer  (business rules — no DB knowledge)
    |
Repository / EF Core  (data access — no HTTP knowledge)
    |
PostgreSQL  (storage — knows nothing about the app)
```

---

## Quick Start

### Prerequisites

- .NET 9 SDK
- PostgreSQL (or Docker)
- Visual Studio 2022 / VS Code

### Run Locally

```bash
# Clone
git clone https://github.com/GawdSizwe/CarvedRockFitness.git
cd CarvedRockFitness

# Start Auth Server (port 5002)
cd CarvedRockFitness.Auth
dotnet run

# Start API (port 5001)
cd ../CarvedRockFitness.API
dotnet run

# Start Web (port 5000)
cd ../CarvedRockFitness.Web
dotnet run
```

---

## Features

### Platform Modules

| Module | Description | Access |
|--------|-------------|--------|
| **Meetups Feed** | Community event listings with pinning and categories | Public |
| **Suggestions** | Submit and vote on community ideas | Authenticated |
| **YVEDA Academy** | Sports academy registrations, stats, reviews | Public / Admin |
| **Transport** | Driver listings, applications, route management | Public / Admin |
| **Ratings** | Star ratings for drivers and academy | Public |
| **Admin Dashboard** | Full platform control panel | Admin only |
| **Ad Banners** | Sponsored banner management | Admin only |

### Security

| Feature | Detail |
|---------|--------|
| **OAuth2 / OpenID Connect** | OpenIddict 5.4 — industry-standard token issuance |
| **JWT Bearer Auth** | Stateless — API validates signature without calling Auth |
| **Role-Based Access** | `Admin` · `PaidUser` · `FreeUser` enforced per endpoint |
| **Per-Endpoint Rate Limiting** | 6 sliding window policies — see Observability section |
| **HTTPS Enforced** | `force_https = true` on all Fly services |
| **CORS Policy** | Scoped to known Web origin only |

---

## Deployment

### Fly.io Production Setup

```bash
# Deploy all three services
flyctl deploy --app gawdsizwe-api
flyctl deploy --app gawdsizwe-web
flyctl deploy --app gawdsizwe-auth
```

### Environment Secrets

```bash
# API
flyctl secrets set DATABASE_URL="postgres://..." --app gawdsizwe-api
flyctl secrets set Auth__Authority="https://gawdsizwe-auth.fly.dev" --app gawdsizwe-api

# Web
flyctl secrets set Api__BaseUrl="https://gawdsizwe-api.fly.dev" --app gawdsizwe-web
flyctl secrets set Auth__Authority="https://gawdsizwe-auth.fly.dev" --app gawdsizwe-web

# Auth
flyctl secrets set ConnectionStrings__DefaultConnection="postgres://..." --app gawdsizwe-auth
```

### Infrastructure

| Component | Spec | Region |
|-----------|------|--------|
| Web App | 1 shared CPU · 256MB RAM | JNB |
| REST API | 1 shared CPU · 512MB RAM | JNB |
| Auth Server | 1 shared CPU · 512MB RAM | JNB |
| PostgreSQL | Fly Managed DB · 1GB | JNB |

---

## Observability

### Health Check Endpoints

| Endpoint | Purpose | Checked By |
|----------|---------|------------|
| `GET /healthz` | All checks combined | Grafana |
| `GET /healthz/live` | Liveness — is the process up? | Fly.io (every 30s) |
| `GET /healthz/ready` | Readiness — is the DB reachable? | Fly.io (every 60s) |

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.012",
  "entries": {
    "self":     { "status": "Healthy" },
    "database": { "status": "Healthy" }
  }
}
```

Fly.io will **automatically restart** any machine that fails a health check — zero manual intervention required.

### Rate Limiting

All policies are **IP-keyed sliding window** — returns `429 Too Many Requests` with `Retry-After: 60` on breach.

| Policy | Endpoint | Limit | Reason |
|--------|----------|-------|--------|
| `general-read` | All public GETs | 100 / 1 min | Prevent scraping |
| `admin` | All admin routes | 30 / 1 min | Block credential stuffing |
| `suggestions-write` | `POST /suggestions` | 5 / 10 min | Anti-spam |
| `ratings-write` | `POST /ratings` | 10 / 10 min | Anti-spam |
| `academy-register` | `POST /academy/register` | 3 / 10 min | One registration per family |
| `drivers-write` | `POST /drivers/apply` | 5 / 10 min | Deliberate action only |

Rate limit breaches are logged as structured warnings — queryable in Fly.io log explorer.

### Structured Logging (Serilog)

```
[INF] HTTP POST /suggestions responded 201 in 42.3ms
[WRN] Rate limit exceeded: POST /academy/register from 196.x.x.x
[ERR] JWT validation failed: token expired
```

---

## Release History

| Tag | Description |
|-----|-------------|
| `v1.0.0` | OIDC baseline + Fly.io deployment |
| `v2.0.0` | Academy, Transport, Ratings, Admin, Driver system |
| `v2.1.0` | Per-endpoint rate limiting + health checks + observability |

---

## Technical Decisions

| Decision | Choice | Why |
|----------|--------|-----|
| Auth framework | OpenIddict 5.4 | Full control, no SaaS dependency, production-grade |
| ORM | EF Core 9 | Type-safe queries, migration support, EF health probes |
| Rate limiting | ASP.NET Core built-in | Zero extra dependencies, middleware-level enforcement |
| Deployment | Fly.io (JNB) | Low latency for SA users, managed PostgreSQL, free tier |
| Logging | Serilog | Structured output, queryable in production |
| Token strategy | Stateless JWT | No session affinity — horizontally scalable |

---

_Built for the KasiMeetups community platform — connecting Tembisa and surrounding areas through sport, transport, and culture._