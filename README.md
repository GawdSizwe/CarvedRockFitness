# KasiMeetups — CarvedRock Fitness Platform

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![C#](https://img.shields.io/badge/C%23-13.0-239120?logo=csharp)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![EF Core](https://img.shields.io/badge/EF_Core-9.0-512BD4?logo=dotnet)](https://docs.microsoft.com/ef/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Latest-4169E1?logo=postgresql)](https://www.postgresql.org)
[![OpenIddict](https://img.shields.io/badge/OpenIddict-5.4-blueviolet)](https://github.com/openiddict/openiddict-core)
[![Fly.io](https://img.shields.io/badge/Fly.io-LIVE-8B5CF6)](https://fly.io)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

> **Enterprise-grade community platform** built on ASP.NET Core 9 — microservices architecture, OAuth2/OpenID Connect identity, role-based access control, per-endpoint rate limiting, and split health checks. Live in Johannesburg (JNB).
>
> _Clean Architecture . OAuth2/OIDC . Role-Based Access . Per-Endpoint Rate Limiting . Live on Fly.io_

[Live Demo](#live-demo) . [Architecture](#architecture) . [Security](#security-architecture) . [Tech Stack](#technical-stack) . [Quick Start](#quick-start) . [Features](#features) . [Observability](#observability) . [Deployment](#deployment) . [Laws](#the-laws-behind-it)

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

| Service | URL | Role |
|---------|-----|------|
| Web App | https://gawdsizwe-web.fly.dev | MVC Frontend |
| REST API | https://gawdsizwe-api.fly.dev | Data + Business Logic |
| Auth Server | https://gawdsizwe-auth.fly.dev | OAuth2 Identity Provider |
| API Health | https://gawdsizwe-api.fly.dev/healthz | All Health Checks |
| API Liveness | https://gawdsizwe-api.fly.dev/healthz/live | Process Alive? |
| API Readiness | https://gawdsizwe-api.fly.dev/healthz/ready | DB Reachable? |

---

## Architecture

### Microservices Topology

Four independently deployed services. Zero shared state. All communication via signed JWT tokens.

```
+------------------------------------------------------------------+
|                       Fly.io Cloud (JNB Region)                  |
|                                                                  |
|  +-------------------+   OIDC/OAuth2   +--------------------+   |
|  |  gawdsizwe-web    | --------------> | gawdsizwe-auth     |   |
|  |                   |                 |                    |   |
|  |  ASP.NET Core 9   |                 | OpenIddict 5.4     |   |
|  |  MVC + Razor      |                 | OAuth2 / OIDC      |   |
|  |  256MB . :8080    |                 | 512MB . :8080      |   |
|  +--------+----------+                 +--------------------+   |
|           |                                                      |
|           | Bearer JWT (signed, stateless)                       |
|           v                                                      |
|  +-------------------+   EF Core 9     +--------------------+   |
|  |  gawdsizwe-api    | --------------> | gym-db             |   |
|  |                   |                 |                    |   |
|  |  ASP.NET Core 9   |                 | PostgreSQL         |   |
|  |  REST Web API     |                 | Fly Managed DB     |   |
|  |  512MB . :8080    |                 | 1GB Storage        |   |
|  +-------------------+                 +--------------------+   |
|                                                                  |
+------------------------------------------------------------------+
```

### Service Responsibilities

| Service | Single Responsibility | Knows About |
|---------|----------------------|-------------|
| **Web** | Render HTML, handle user sessions | Auth + API endpoints only |
| **API** | Business logic, data access, validation | Database only |
| **Auth** | Issue and validate tokens | Users + roles only |
| **Database** | Store data | Nothing else |

> The frontend has never written a SQL query.
> The database has never seen an HTTP request.
> The auth server has never heard of meetups.
> That is separation of concerns enforced at the deployment boundary.

---

### N-Tier Internal Structure (per service)

```
+-----------------------------------------------+
|              Controller Layer                  |
|   - Receives HTTP requests                    |
|   - Returns HTTP responses                    |
|   - Zero business logic                       |
+------------------------+----------------------+
                         |
                         v
+-----------------------------------------------+
|              Service Layer                     |
|   - Business rules and validation             |
|   - Orchestration between repositories        |
|   - No HTTP knowledge, no DB knowledge        |
+------------------------+----------------------+
                         |
                         v
+-----------------------------------------------+
|           Repository / EF Core 9               |
|   - Database queries via LINQ                 |
|   - Entity mapping and migrations             |
|   - No business logic, no HTTP knowledge      |
+------------------------+----------------------+
                         |
                         v
+-----------------------------------------------+
|              PostgreSQL (Fly Managed)           |
|   - Storage only                              |
|   - Knows nothing about HTTP or business rules|
+-----------------------------------------------+
```

> Each tier only talks to the one directly below it.
> A Controller never writes SQL. The database never knows about HTTP.

---

## Security Architecture

### Authentication Flow

```
+----------+        +------------+        +----------+
|  Client  | -----> |   Auth     | -----> | Returns  |
| (Browser)|  login |   Server   |  token |   JWT    |
+----------+        +------------+        +----+-----+
                                               |
                                    signed token (1hr TTL)
                                               |
                                               v
                                         +----------+
                                         |   API    |
                                         | validates|
                                         | signature|
                                         | locally  |
                                         +----+-----+
                                              |
                                    authorized by role?
                                     +--------+--------+
                                     |                 |
                                    YES                NO
                                     |                 |
                                +----v-----+     +-----v------+
                                |  200 OK  |     |    401     |
                                |  Data    |     | Unauthorized|
                                +----------+     +------------+
```

> The API validates the JWT signature locally.
> It never calls the Auth server on every request.
> That is zero auth latency after token issuance.

### Role-Based Access Control

| Role | Access Level | Example Actions |
|------|-------------|-----------------|
| **Admin** | Full platform control | Manage drivers, approve registrations, toggle settings |
| **PaidUser** | Enhanced features | Submit suggestions, access premium content |
| **FreeUser** | Public access | View meetups, browse drivers, read reviews |

---

## Technical Stack

| Technology | Version | Purpose |
|-----------|---------|---------|
| ASP.NET Core | 9.0 | Web framework (Web + API + Auth) |
| C# | 13.0 | Programming language |
| Entity Framework Core | 9.0 | ORM + database migrations |
| PostgreSQL | Latest | Primary database (Fly managed) |
| OpenIddict | 5.4 | OAuth2 / OpenID Connect provider |
| Serilog | Latest | Structured logging |
| Redis | Optional | Distributed caching (fallback: in-memory) |
| Razor Views | Built-in | Server-side UI rendering |
| Bootstrap | 5.x | Responsive CSS framework |

---

## Quick Start

### Prerequisites

- .NET 9 SDK
- PostgreSQL (or Docker)
- Visual Studio 2022 / VS Code / Rider

### Run Locally

```bash
# Clone
git clone https://github.com/GawdSizwe/CarvedRockFitness.git
cd CarvedRockFitness

# Start Auth Server (terminal 1)
cd CarvedRockFitness.Auth
dotnet run

# Start API (terminal 2)
cd CarvedRockFitness.API
dotnet run

# Start Web (terminal 3)
cd CarvedRockFitness.Web
dotnet run
```

---

## Features

### Platform Modules

| Module | Description | Access |
|--------|-------------|--------|
| **Community Feed** | Event listings with pinning and categories | Public |
| **Suggestions** | Submit and vote on community ideas | Authenticated |
| **YVEDA Academy** | Sports registrations, stats, reviews | Public / Admin |
| **Transport** | Driver listings, applications, routes | Public / Admin |
| **Ratings** | Star ratings for drivers and academy | Public |
| **Admin Dashboard** | Full platform control panel | Admin only |
| **Ad Banners** | Sponsored banner management | Admin only |

---

## Observability

### Health Check Endpoints

```
Fly.io ----> GET /healthz/live  ----> Process alive?  ----> YES / NO
              (every 30s)

Fly.io ----> GET /healthz/ready ----> DB reachable?   ----> YES / NO
              (every 60s)                                      |
                                                          NO = auto restart

Grafana ---> GET /healthz       ----> All checks       ----> JSON response
```

| Endpoint | Purpose | Checked By |
|----------|---------|------------|
| `GET /healthz` | All checks combined | Grafana |
| `GET /healthz/live` | Liveness -- is the process up? | Fly.io (every 30s) |
| `GET /healthz/ready` | Readiness -- is the DB reachable? | Fly.io (every 60s) |

Response format:

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.012",
  "entries": {
    "self":     { "status": "Healthy", "description": "API is running" },
    "database": { "status": "Healthy" }
  }
}
```

> Fly.io will automatically restart any machine that fails a health check.
> Zero manual intervention required.

### Rate Limiting

```
Request ----> [Rate Limiter] ----> Allowed? ----> [Controller] ----> Response
                   |
                   NO
                   |
                   v
             429 Too Many Requests
             Retry-After: 60
             Logged: IP + Path + Method
```

All policies are **IP-keyed sliding window** -- returns `429 Too Many Requests` with `Retry-After: 60` on breach.

| Policy | Endpoint | Limit | Threat |
|--------|----------|-------|--------|
| `general-read` | All public GETs | 100 / 1 min | Scraping |
| `admin` | All admin routes | 30 / 1 min | Credential stuffing |
| `suggestions-write` | POST /suggestions | 5 / 10 min | Spam |
| `ratings-write` | POST /ratings | 10 / 10 min | Fake reviews |
| `academy-register` | POST /academy/register | 3 / 10 min | Bot registrations |
| `drivers-write` | POST /drivers/apply | 5 / 10 min | Application flooding |

### Structured Logging (Serilog)

```
[INF] HTTP POST /suggestions responded 201 in 42.3ms
[WRN] Rate limit exceeded: POST /academy/register from 196.x.x.x
[ERR] JWT validation failed: token expired
```

> Every log entry is a queryable data point -- not a string.
> Method, path, status, duration, and client IP are discrete fields.

---

## Deployment

### Fly.io Production (JNB Region)

```bash
# Deploy each service from its own directory
cd CarvedRockFitness.API  && flyctl deploy
cd CarvedRockFitness.Web  && flyctl deploy
cd CarvedRockFitness.Auth && flyctl deploy
```

### Infrastructure

| Component | Spec | Region |
|-----------|------|--------|
| Web App | 1 shared CPU . 256MB RAM | JNB |
| REST API | 1 shared CPU . 512MB RAM | JNB |
| Auth Server | 1 shared CPU . 512MB RAM | JNB |
| PostgreSQL | Fly Managed DB . 1GB | JNB |

### Environment Secrets

```bash
flyctl secrets set DATABASE_URL="postgres://..." --app gawdsizwe-api
flyctl secrets set Auth__Authority="https://gawdsizwe-auth.fly.dev" --app gawdsizwe-api
flyctl secrets set Api__BaseUrl="https://gawdsizwe-api.fly.dev" --app gawdsizwe-web
```

---

## The Laws Behind It

Five foundational laws of computer science -- all five enforced at the infrastructure level, not just in code.

| Law | Author | Year | How It Is Applied |
|-----|--------|------|-------------------|
| **Conway's Law** | Mel Conway | 1967 | 4 service boundaries designed before code -- the architecture mirrors 4 independent teams in 1 developer |
| **Separation of Concerns** | Edsger Dijkstra | 1974 | The database has never seen HTTP. The frontend has never executed SQL. Hard boundaries, not soft suggestions. |
| **Principle of Least Privilege** | Jerome Saltzer | 1975 | One service holds the DB connection string. The other three do not know the database exists. |
| **Fallacies of Distributed Computing** | Peter Deutsch | 1994 | Health probes every 30s. Auto-restart on failure. Designed for the network to lie. |
| **Single Responsibility Principle** | Robert C. Martin | 2003 | Each service has exactly one reason to change. Auth ships independently of API. API ships independently of Web. |

---

## How It Compares

| Company | Their Architecture | This Platform |
|---------|-------------------|---------------|
| **Netflix** | Microservices, JWT, per-service auth, health gates | Same pattern. Different scale. |
| **Uber** | Service isolation, stateless requests, structured logs | Same pattern. Different scale. |
| **FNB / Standard Bank** | OAuth2, RBAC, least privilege, audit trail | Same pattern. Same country. |
| **Seed-Stage Startups** | Monolith, basic JWT, no health checks | Already ahead of most funded companies. |
| **Bootcamp Graduates** | CRUD, session auth, Heroku, no rate limiting | Three levels below this architecture. |

---

## Technical Decisions

| Decision | Choice | Why |
|----------|--------|-----|
| Auth framework | OpenIddict 5.4 | Full control, no SaaS dependency, production-grade |
| ORM | EF Core 9 | Type-safe queries, migration support, health probes |
| Rate limiting | ASP.NET Core built-in | Zero extra dependencies, middleware-level enforcement |
| Deployment | Fly.io (JNB) | Low latency for SA users, managed PostgreSQL, free tier |
| Logging | Serilog | Structured output, queryable in production |
| Token strategy | Stateless JWT | No session affinity -- horizontally scalable |
| Health checks | ASP.NET + HealthChecks.UI | JSON responses, split probes, Grafana-compatible |

---

## Release History

| Tag | Description |
|-----|-------------|
| `v1.0.0` | OIDC baseline + Fly.io deployment |
| `v2.0.0` | Academy, Transport, Ratings, Admin, Driver system |
| `v2.1.0` | Per-endpoint rate limiting + split health checks + observability |

---

> _Five laws. Written between 1967 and 2003._
> _All five enforced at the infrastructure level._
> _Not in a textbook. Not in a tutorial._
> _Running in production. Johannesburg. Right now._
>
> _One platform. One developer. Tembisa._

---

_Built for the KasiMeetups community -- connecting Tembisa and surrounding areas through sport, transport, and culture._