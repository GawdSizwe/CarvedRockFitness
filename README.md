<div align="center">

# KasiMeetups — CarvedRock Fitness Platform

### Enterprise-Grade Community Platform

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com)
[![C#](https://img.shields.io/badge/C%23-13.0-239120?style=for-the-badge&logo=csharp)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![EF Core](https://img.shields.io/badge/EF_Core-9.0-512BD4?style=for-the-badge&logo=dotnet)](https://docs.microsoft.com/ef/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Latest-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org)
[![OpenIddict](https://img.shields.io/badge/OpenIddict-5.4-blueviolet?style=for-the-badge)](https://github.com/openiddict/openiddict-core)
[![Fly.io](https://img.shields.io/badge/Fly.io-LIVE-8B5CF6?style=for-the-badge)](https://fly.io)
[![License](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)](LICENSE)

---

**Microservices Architecture | OAuth2/OpenID Connect | Role-Based Access Control**
**Per-Endpoint Rate Limiting | Split Health Probes | Structured Observability**

---

*Built on ASP.NET Core 9 -- four independently deployed microservices,*
*secured by a self-hosted OAuth2 identity server, protected by six per-endpoint*
*rate limiting policies, monitored by Kubernetes-style health probes,*
*and deployed to the Johannesburg (JNB) region on Fly.io.*

---

[Live Demo](#live-demo) | [Architecture](#architecture) | [Security](#security-architecture) | [Tech Stack](#technical-stack) | [Quick Start](#quick-start) | [Features](#features) | [Observability](#observability) | [Deployment](#deployment) | [Laws](#the-laws-behind-it)

---

### Built By

**Sizwe Mazibuko** -- Software Engineer / Tech Lead

[![GitHub](https://img.shields.io/badge/GitHub-GawdSizwe-181717?style=for-the-badge&logo=github)](https://github.com/GawdSizwe)
[![Email](https://img.shields.io/badge/Email-smazibuko01%40gmail.com-EA4335?style=for-the-badge&logo=gmail&logoColor=white)](mailto:smazibuko01@gmail.com)

</div>

---

## Live Demo

<div align="center">

| Service | URL | Role | Status |
|:-------:|:---:|:----:|:------:|
| **Web App** | [gawdsizwe-web.fly.dev](https://gawdsizwe-web.fly.dev) | MVC Frontend | LIVE |
| **REST API** | [gawdsizwe-api.fly.dev](https://gawdsizwe-api.fly.dev) | Data + Business Logic | LIVE |
| **Auth Server** | [gawdsizwe-auth.fly.dev](https://gawdsizwe-auth.fly.dev) | OAuth2 Identity Provider | LIVE |
| **API Health** | [/healthz](https://gawdsizwe-api.fly.dev/healthz) | All Health Checks | LIVE |
| **Liveness** | [/healthz/live](https://gawdsizwe-api.fly.dev/healthz/live) | Process Alive? | LIVE |
| **Readiness** | [/healthz/ready](https://gawdsizwe-api.fly.dev/healthz/ready) | DB Reachable? | LIVE |

</div>

---

## Architecture

### Microservices Topology

<div align="center">

**Four independently deployed services. Zero shared state. All communication via signed JWT tokens.**

</div>

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

<div align="center">

### Service Responsibilities

| Service | Single Responsibility | Knows About |
|:-------:|:--------------------:|:-----------:|
| **Web** | Render HTML, handle user sessions | Auth + API endpoints only |
| **API** | Business logic, data access, validation | Database only |
| **Auth** | Issue and validate tokens | Users + roles only |
| **Database** | Store data | Nothing else |

</div>

> *The frontend has never written a SQL query.*
> *The database has never seen an HTTP request.*
> *The auth server has never heard of meetups.*
> *That is separation of concerns enforced at the deployment boundary.*

---

### N-Tier Internal Structure

<div align="center">

**Each service follows strict layered architecture -- each tier only talks to the one below it.**

</div>

```
+-----------------------------------------------+
|              CONTROLLER LAYER                  |
|   Receives HTTP requests                      |
|   Returns HTTP responses                      |
|   Zero business logic                         |
+------------------------+----------------------+
                         |
                         v
+-----------------------------------------------+
|              SERVICE LAYER                     |
|   Business rules and validation               |
|   Orchestration between repositories          |
|   No HTTP knowledge, no DB knowledge          |
+------------------------+----------------------+
                         |
                         v
+-----------------------------------------------+
|           REPOSITORY / EF CORE 9               |
|   Database queries via LINQ                   |
|   Entity mapping and migrations               |
|   No business logic, no HTTP knowledge        |
+------------------------+----------------------+
                         |
                         v
+-----------------------------------------------+
|              POSTGRESQL (FLY MANAGED)          |
|   Storage only                                |
|   Knows nothing about HTTP or business rules  |
+-----------------------------------------------+
```

---

## Security Architecture

### Authentication Flow

```
+----------+        +------------+        +----------+
|  CLIENT  | -----> |    AUTH    | -----> |  RETURNS |
| (Browser)|  login |   SERVER   |  signs |   JWT    |
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

<div align="center">

> **The API validates the JWT signature locally. It never calls the Auth server per request.**
> **That is zero auth latency after token issuance.**

</div>

### Role-Based Access Control

<div align="center">

| Role | Access Level | Example Actions |
|:----:|:-----------:|:---------------:|
| **Admin** | Full platform control | Manage drivers, approve registrations, toggle settings |
| **PaidUser** | Enhanced features | Submit suggestions, access premium content |
| **FreeUser** | Public access | View meetups, browse drivers, read reviews |

</div>

---

## Technical Stack

<div align="center">

| Technology | Version | Purpose |
|:----------:|:-------:|:-------:|
| **ASP.NET Core** | 9.0 | Web framework (Web + API + Auth) |
| **C#** | 13.0 | Programming language |
| **Entity Framework Core** | 9.0 | ORM + database migrations |
| **PostgreSQL** | Latest | Primary database (Fly managed) |
| **OpenIddict** | 5.4 | OAuth2 / OpenID Connect provider |
| **Serilog** | Latest | Structured logging |
| **Redis** | Optional | Distributed caching (fallback: in-memory) |
| **Razor Views** | Built-in | Server-side UI rendering |
| **Bootstrap** | 5.x | Responsive CSS framework |

</div>

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
cd CarvedRockFitness.Auth && dotnet run

# Start API (terminal 2)
cd CarvedRockFitness.API && dotnet run

# Start Web (terminal 3)
cd CarvedRockFitness.Web && dotnet run
```

---

## Features

### Platform Modules

<div align="center">

| Module | Description | Access |
|:------:|:-----------:|:------:|
| **Community Feed** | Event listings with pinning and categories | Public |
| **Suggestions** | Submit and vote on community ideas | Authenticated |
| **YVEDA Academy** | Sports registrations, stats, reviews | Public / Admin |
| **Transport** | Driver listings, applications, routes | Public / Admin |
| **Ratings** | Star ratings for drivers and academy | Public |
| **Admin Dashboard** | Full platform control panel | Admin only |
| **Ad Banners** | Sponsored banner management | Admin only |

</div>

---

## Observability

### Health Check Architecture

```
FLY.IO -----> GET /healthz/live  -----> Process alive?  -----> YES / NO
               (every 30s)

FLY.IO -----> GET /healthz/ready -----> DB reachable?   -----> YES / NO
               (every 60s)                                       |
                                                            NO = AUTO RESTART

GRAFANA ----> GET /healthz       -----> All checks      -----> JSON response
```

<div align="center">

| Endpoint | Purpose | Checked By | Interval |
|:--------:|:-------:|:----------:|:--------:|
| **`/healthz`** | All checks combined | Grafana | On demand |
| **`/healthz/live`** | Liveness -- process up? | Fly.io | Every 30s |
| **`/healthz/ready`** | Readiness -- DB reachable? | Fly.io | Every 60s |

</div>

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

<div align="center">

> **Fly.io automatically restarts any machine that fails a health check.**
> **Zero manual intervention required.**

</div>

---

### Rate Limiting Architecture

```
REQUEST -----> [RATE LIMITER] -----> Allowed? -----> [CONTROLLER] -----> RESPONSE
                     |
                     NO
                     |
                     v
               429 TOO MANY REQUESTS
               Retry-After: 60
               Logged: IP + Path + Method
```

<div align="center">

**IP-keyed sliding window -- returns 429 with Retry-After: 60 on breach.**

| Policy | Endpoint | Limit | Threat Addressed |
|:------:|:--------:|:-----:|:----------------:|
| **`general-read`** | All public GETs | 100 / 1 min | Data scraping |
| **`admin`** | All admin routes | 30 / 1 min | Credential stuffing |
| **`suggestions-write`** | POST /suggestions | 5 / 10 min | Content spam |
| **`ratings-write`** | POST /ratings | 10 / 10 min | Fake reviews |
| **`academy-register`** | POST /academy/register | 3 / 10 min | Bot registrations |
| **`drivers-write`** | POST /drivers/apply | 5 / 10 min | Application flooding |

</div>

---

### Structured Logging

```
[INF] HTTP POST /suggestions responded 201 in 42.3ms
[WRN] Rate limit exceeded: POST /academy/register from 196.x.x.x
[ERR] JWT validation failed: token expired
```

<div align="center">

> **Every log entry is a queryable data point -- not a string.**
> **Method, path, status, duration, and client IP are discrete fields.**

</div>

---

## Deployment

### Infrastructure

<div align="center">

| Component | Spec | Region | Status |
|:---------:|:----:|:------:|:------:|
| **Web App** | 1 shared CPU . 256MB RAM | JNB | Deployed |
| **REST API** | 1 shared CPU . 512MB RAM | JNB | Deployed |
| **Auth Server** | 1 shared CPU . 512MB RAM | JNB | Deployed |
| **PostgreSQL** | Fly Managed DB . 1GB | JNB | Deployed |

</div>

### Deploy Commands

```bash
cd CarvedRockFitness.API  && flyctl deploy
cd CarvedRockFitness.Web  && flyctl deploy
cd CarvedRockFitness.Auth && flyctl deploy
```

---

## The Laws Behind It

<div align="center">

**Five foundational laws of computer science -- all five enforced at the infrastructure level.**

| Law | Author | Year | How It Is Applied |
|:---:|:------:|:----:|:-----------------:|
| **Conway's Law** | Mel Conway | 1967 | 4 service boundaries designed before code -- mirrors 4 independent teams |
| **Separation of Concerns** | Edsger Dijkstra | 1974 | DB never sees HTTP. Frontend never executes SQL. Hard boundaries. |
| **Least Privilege** | Jerome Saltzer | 1975 | One service holds the DB connection string. Three others don't know it exists. |
| **Fallacies of Distributed Computing** | Peter Deutsch | 1994 | Health probes every 30s. Auto-restart on failure. Designed for network failure. |
| **Single Responsibility** | Robert C. Martin | 2003 | Each service has one reason to change. Auth ships independently of API. |

</div>

---

## How It Compares

<div align="center">

| Company | Their Architecture | This Platform |
|:-------:|:------------------:|:-------------:|
| **Netflix** | Microservices, JWT, per-service auth, health gates | **Same pattern. Different scale.** |
| **Uber** | Service isolation, stateless requests, structured logs | **Same pattern. Different scale.** |
| **FNB / Standard Bank** | OAuth2, RBAC, least privilege, audit trail | **Same pattern. Same country.** |
| **Seed-Stage Startups** | Monolith, basic JWT, no health checks | **Already ahead.** |
| **Bootcamp Graduates** | CRUD, session auth, Heroku, no rate limiting | **Three levels behind.** |

</div>

---

## Technical Decisions

<div align="center">

| Decision | Choice | Why |
|:--------:|:------:|:---:|
| **Auth** | OpenIddict 5.4 | Full control, no SaaS dependency, production-grade |
| **ORM** | EF Core 9 | Type-safe queries, migrations, health probes |
| **Rate Limiting** | ASP.NET Core built-in | Zero extra dependencies, middleware enforcement |
| **Deployment** | Fly.io (JNB) | Low latency for SA, managed PostgreSQL, free tier |
| **Logging** | Serilog | Structured output, queryable in production |
| **Tokens** | Stateless JWT | No session affinity -- horizontally scalable |
| **Health** | ASP.NET + HealthChecks.UI | JSON responses, split probes, Grafana-ready |

</div>

---

## Release History

<div align="center">

| Tag | Description |
|:---:|:-----------:|
| **`v1.0.0`** | OIDC baseline + Fly.io deployment |
| **`v2.0.0`** | Academy, Transport, Ratings, Admin, Driver system |
| **`v2.1.0`** | Per-endpoint rate limiting + split health checks + observability |

</div>

---

<div align="center">

### Five laws. Written between 1967 and 2003.

### All five enforced at the infrastructure level.

### Not in a textbook. Not in a tutorial.

### Running in production. Johannesburg. Right now.

---

**One platform. One developer. Tembisa.**

---

_Built for the KasiMeetups community -- connecting Tembisa and surrounding areas through sport, transport, and culture._

</div>