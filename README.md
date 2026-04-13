# KasiMeetups — CarvedRock Fitness Platform

<div align="center">

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-13.0-239120?style=for-the-badge&logo=csharp&logoColor=white)
![EF Core](https://img.shields.io/badge/EF_Core-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)
![OpenIddict](https://img.shields.io/badge/OpenIddict-5.4-blue?style=for-the-badge)
![Fly.io](https://img.shields.io/badge/Fly.io-Live-8B5CF6?style=for-the-badge)

**Enterprise-grade fitness community platform built on ASP.NET Core 9**

*Clean Architecture · OAuth2/OpenID Connect · Role-Based Access · Live on Fly.io*

[Live Demo](#-live-demo) •
[Architecture](#-architecture) •
[Quick Start](#-quick-start) •
[Features](#-features) •
[Deployment](#-deployment)

</div>

---

## ?? Author

| | |
|---|---|
| **Sizwe Mazibuko** | Software Engineer / Tech Lead |
| GitHub | [github.com/GawdSizwe](https://github.com/GawdSizwe) |
| Email | smazibuko01@gmail.com |

---

## ?? Live Demo

| Service | URL |
|---------|-----|
| **Web App** | https://gawdsizwe-web.fly.dev |
| **Auth Server** | https://gawdsizwe-auth.fly.dev |
| **API** | https://gawdsizwe-api.fly.dev |

### Demo Credentials

| Username | Password | Role | Access |
|----------|----------|------|--------|
| `admin@kasimeetups.com` | `Admin123!` | Admin | Full platform access, admin dashboard, user management |
| `paid@kasimeetups.com` | `Paid123!` | PaidUser | Academy, transport, ratings, suggestions |
| `free@kasimeetups.com` | `Free123!` | FreeUser | Browse only, upgrade prompts |

> Register a new account at `/account/register` to test the self-service flow.

---

## ?? Architecture

```
+-----------------------------------------------------+
¦                   Fly.io Cloud                      ¦
¦                                                     ¦
¦  +--------------+  OIDC   +----------------------+ ¦
¦  ¦  Web App     ¦?-------?¦  Auth Server         ¦ ¦
¦  ¦  (MVC/Razor) ¦         ¦  (OpenIddict 5.4)    ¦ ¦
¦  ¦  :8080       ¦         ¦  :8080               ¦ ¦
¦  +--------------+         +----------------------+ ¦
¦         ¦ Bearer JWT                                ¦
¦         ?                                           ¦
¦  +--------------+         +----------------------+ ¦
¦  ¦  REST API    ¦?-------?¦  PostgreSQL (Fly DB) ¦ ¦
¦  ¦  (Web API)   ¦         ¦  gym-db              ¦ ¦
¦  ¦  :8080       ¦         +----------------------+ ¦
¦  +--------------+                                   ¦
+-----------------------------------------------------+
```

### Projects

| Project | Purpose | Port (local) |
|---------|---------|-------------|
| `CarvedRockFitness.Web` | MVC Razor Pages client app | 5001 |
| `CarvedRockFitness.API` | REST API + business logic | 5002 |
| `CarvedRockFitness.Auth` | OpenIddict identity server | 5003 |

---

## ? Features

### Platform Modules

| Module | Route | Role Required |
|--------|-------|---------------|
| Home / Feed | `/` | Any authenticated |
| **Academy** | `/academy` | PaidUser / Admin |
| Academy Registration | `/academy/register` | PaidUser / Admin |
| **Suggestions** | `/suggestion/create` | PaidUser / Admin |
| **Ad Banners** | `/admin` | Admin |
| **Admin Dashboard** | `/admin` | Admin |
| **Transport** | `/transport` | PaidUser / Admin |
| **Ratings** | `/rating` | PaidUser / Admin |

### Security & Auth

- OAuth2 Authorization Code flow with PKCE (OpenIddict 5.4)
- Role-based access control (`Admin`, `PaidUser`, `FreeUser`)
- JWT Bearer token validation on API
- HTTPS enforcement + `ForwardedHeaders` for Fly.io proxy
- Cookie authentication with sliding expiry on Web

### Infrastructure

- Structured logging with Serilog (console + file)
- Health check endpoints (`/health`)
- EF Core with PostgreSQL (Fly Postgres)
- Fly.io multi-region deployment
- Docker-based builds via `fly deploy`

---

## ?? Quick Start (Local)

### Prerequisites

- .NET 9 SDK
- PostgreSQL (or use SQLite by swapping connection string)

### 1. Clone & Configure

```bash
git clone https://github.com/GawdSizwe/CarvedRockFitness.git
cd CarvedRockFitness
```

### 2. Set User Secrets

```powershell
# Auth Server
cd CarvedRockFitness.Auth
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=carvedrock;Username=postgres;Password=yourpassword"

# API
cd ..\CarvedRockFitness.API
dotnet user-secrets set "ConnectionStrings:CarvedRockFitnessDbContextConnection" "Host=localhost;Database=carvedrock;Username=postgres;Password=yourpassword"
```

### 3. Run all three

```powershell
# Terminal 1 — Auth
cd CarvedRockFitness.Auth && dotnet run

# Terminal 2 — API
cd CarvedRockFitness.API && dotnet run

# Terminal 3 — Web
cd CarvedRockFitness.Web && dotnet run
```

Navigate to `https://localhost:5001`

---

## ?? Data Model (Key Entities)

| Entity | Description |
|--------|-------------|
| `ApplicationUser` | Extended Identity user with `IsPaidUser` flag |
| `AcademyRegistration` | Fitness class enrolment |
| `AcademyReview` | Rating/review per class |
| `Driver` | Transport driver profile |
| `DriverApplication` | Driver onboarding request |
| `DriverRating` | Star rating for a trip |
| `AdBanner` | Admin-managed promotional banners |
| `Suggestion` | Community improvement suggestion |

---

## ?? Testing

```bash
# Run all tests
dotnet test

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

| Suite | Count | Focus |
|-------|-------|-------|
| `UserService` | 7 | CRUD, caching, exceptions |
| `RoutineService` | 5 | Queries, ownership |
| `UsersController` | 6 | HTTP responses |
| `Integration` | 7 | End-to-end |
| **Total** | **26** | |

---

## ?? Deployment (Fly.io)

### Deploy Commands

```powershell
$env:PATH += ";$env:USERPROFILE\.fly\bin"

# Auth
cd CarvedRockFitness.Auth
flyctl deploy --remote-only --app gawdsizwe-auth --wait-timeout 300

# API
cd ..\CarvedRockFitness.API
flyctl deploy --remote-only --app gawdsizwe-api --wait-timeout 300

# Web
cd ..\CarvedRockFitness.Web
flyctl deploy --remote-only --app gawdsizwe-web --wait-timeout 300
```

### Production Secrets

| App | Secret | Value |
|-----|--------|-------|
| Auth | `DATABASE_URL` | Fly Postgres URL |
| Auth | `SIGNING_CERT_PASSWORD` | RSA cert password |
| API | `DATABASE_URL` | Fly Postgres URL |
| API | `Auth:Authority` | `https://gawdsizwe-auth.fly.dev` |
| Web | `OIDC:Authority` | `https://gawdsizwe-auth.fly.dev` |
| Web | `Api:BaseUrl` | `https://gawdsizwe-api.fly.dev` |
| Web | `PayFast:MerchantId` | PayFast merchant ID |
| Web | `PayFast:MerchantKey` | PayFast merchant key |

### Useful Commands

```powershell
# Logs
flyctl logs --app gawdsizwe-web

# DB access
flyctl postgres connect -a gym-db -d <dbname>

# Promote user to Admin
INSERT INTO "AspNetUserRoles" ("UserId","RoleId")
SELECT 'USER_ID', "Id" FROM "AspNetRoles" WHERE "Name"='Admin';

# Mark user as PaidUser
UPDATE "AspNetUsers" SET "IsPaidUser"=true WHERE "Id"='USER_ID';
```

---

## ?? Known Issues & Fixes

| Issue | Root Cause | Fix |
|-------|------------|-----|
| OIDC redirect_uri mismatch | `http://` detected behind Fly proxy | `ForwardedHeaders` middleware + cleared `KnownNetworks` |
| Token endpoint HTTP method invalid | `UseHttpsRedirection` converted POST?GET | Removed from Auth, rely on proxy |
| ResponseMode form_post failure | Fly proxy blocks POST callbacks | Changed to `ResponseMode.Query` |
| API nvarchar migration error | SQL Server migrations on PostgreSQL | `EnsureCreated` + try/catch skip |

---

## ?? Tech Stack

| Category | Technology | Version |
|----------|------------|---------|
| Runtime | .NET | 9.0 |
| Language | C# | 13 |
| Web Framework | ASP.NET Core MVC | 9.0 |
| Identity | OpenIddict | 5.4 |
| ORM | EF Core | 9.0 |
| Database | PostgreSQL | 16 |
| Logging | Serilog | 8.0 |
| UI | Bootstrap | 5.3 |
| Deployment | Fly.io | — |

---

## ?? License

MIT License — © 2026 Sizwe Mazibuko

---

<div align="center">

**Designed & Developed by Sizwe Mazibuko**

*KasiMeetups / CarvedRock Fitness — Enterprise .NET Demo*

</div>
