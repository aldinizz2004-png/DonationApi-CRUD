# Donation Management API

ProGineer Backend Technical Task 5 (CRUD) · **Izzaldeen** · ASP.NET Core (.NET 10) · EF Core 10 · SQL Server

A RESTful API to create and manage donations. **If a donation is anonymous, the donor's name is never exposed in any API response.**

**Results:** 6 endpoints · 18 Postman requests · **42 / 42 tests passed** · full walkthrough in `DonationApi_Task5_Presentation.pdf`

---

## 1. Tech stack

| Layer | Technology |
| --- | --- |
| Framework | ASP.NET Core Web API (.NET 10), Controllers |
| Data access | Entity Framework Core 10 (Code-First + Migrations) |
| Database | SQL Server Express (local) |
| API docs | Swagger / OpenAPI (Swashbuckle) |
| Testing | Postman collection (v2.1) |

## 2. Project structure

```
DonationApi/
├── Controllers/
│   ├── DonationsController.cs   # CRUD endpoints, HTTP status codes only
│   └── HealthController.cs      # GET /api/health (checks DB connection)
├── DTOs/
│   ├── CreateDonationRequest.cs # POST body + validation rules
│   ├── UpdateDonationRequest.cs # PUT body + validation rules
│   └── DonationResponse.cs      # What the client receives (name masked if anonymous)
├── Models/
│   └── Donation.cs              # Database entity
├── Services/
│   ├── IDonationService.cs
│   └── DonationService.cs       # Business logic + the single entity -> response mapping
├── Data/
│   └── AppDbContext.cs          # Table design: types, constraints, index
├── Migrations/                  # EF Core migration (InitialCreate)
├── Postman/
│   └── Donation API.postman_collection.json
├── Program.cs                   # DI, ProblemDetails, exception handler, Swagger
└── appsettings.json             # Connection string
```

## 3. Setup and run

**Prerequisites:** .NET 10 SDK, SQL Server Express (or any SQL Server instance), EF Core CLI (`dotnet tool install --global dotnet-ef`).

**1. Configure the database.** The connection string is in `appsettings.json`:

```json
"ConnectionStrings": {
  "Default": "Server=.\\SQLEXPRESS;Database=DonationDb;Trusted_Connection=True;TrustServerCertificate=True"
}
```

Change `Server=` if your instance has a different name (e.g. `(localdb)\\MSSQLLocalDB`).

**2. Create the database** (applies the migration):

```bash
dotnet ef database update
```

**3. Run the API:**

```bash
dotnet run --launch-profile http
```

The API listens on `http://localhost:5207`.

## 4. Swagger

Open **http://localhost:5207/swagger** (Development environment).

All endpoints, the request/response models and every possible status code (`[ProducesResponseType]`) are documented, and each endpoint can be tested with **Try it out**.

## 5. Endpoints

| Method | Route | Description | Status codes |
| --- | --- | --- | --- |
| GET | `/api/donations` | All donations, newest first (anonymous names masked) | 200 |
| GET | `/api/donations/{id}` | One donation | 200 · 400 · 404 |
| POST | `/api/donations` | Create a donation | 201 (+ `Location` header) · 400 |
| PUT | `/api/donations/{id}` | Update a donation | 200 · 400 · 404 |
| DELETE | `/api/donations/{id}` | Delete a donation | 204 · 400 · 404 |
| GET | `/api/health` | API + database health | 200 · 503 |

Request body for POST / PUT:

```json
{ "amount": 120.5, "donorName": "Secret Donor", "isAnonymous": true }
```

Response (anonymous):

```json
{ "id": 6, "amount": 120.5, "donorName": "Anonymous", "isAnonymous": true, "createdAt": "2026-10-25T06:42:40.97Z" }
```

## 6. Postman testing

1. Open Postman (desktop app, or web with the **Postman Desktop Agent** so it can reach `localhost`).
2. **Import** `Postman/Donation API.postman_collection.json`.
3. Make sure the API is running. The collection variable `baseUrl` is `http://localhost:5207`.
4. Open the collection → **Run** → **Start run**.

Expected result: **18 requests, 42 tests passed, 0 failed.**

The `Create` requests store the new ids in `normalId` and `anonId`, which the Read / Update / Delete requests reuse, so the collection runs end-to-end and cleans up the data it creates.

| Scenario | Requests | Expected |
| --- | --- | --- |
| Health | Health Check | 200, `Healthy` |
| Create | Normal donation · Anonymous donation | 201, name visible / `Anonymous` |
| Read | Get all · Get normal by id · Get anonymous by id | 200, anonymous name masked, real name absent from body |
| Update | Normal · Anonymous (new real name) | 200, still masked after update |
| Delete | Delete normal → Get deleted → Delete anonymous | 204, then 404 |
| Validation | Negative amount · Zero amount · Missing fields | 400 with field errors |
| Errors | Non-numeric id · Non-existent id (GET / PUT / DELETE) | 400 / 404 |

## 7. Key design decisions

**Anonymity enforced in one place.** `DonationService.ToResponse()` is the only code that turns a `Donation` entity into a `DonationResponse`, and every endpoint returns through it. When `IsAnonymous` is true it returns `"Anonymous"` instead of the name, so no endpoint can leak it. The real name is still stored, so an admin keeps the record and the name reappears if the donation is later made public.

**DTOs instead of the entity.** Requests can't set `Id` or `CreatedAt`. Request fields are nullable (`decimal?`, `bool?`) with `[Required]`, so a missing `amount` or `isAnonymous` returns 400 instead of silently becoming `0` / `false`.

**Thin controller, service layer, no repository.** Controllers only map results to HTTP status codes; the service holds the logic. `DbContext` already acts as a unit of work and repository, so an extra repository layer would be over-engineering.

**Consistent errors.** All errors use RFC 7807 `ProblemDetails` (`AddProblemDetails`, `UseExceptionHandler`, `UseStatusCodePages`). Unhandled exceptions return 500 without leaking a stack trace.

**Id validation.** The route is `{id}`, not `{id:int}`, so `/api/donations/abc` returns **400** (bad input) instead of **404**. Ids ≤ 0 also return 400.

**Health check includes the database.** `GET /api/health` returns 200 only when the database is reachable, otherwise 503.

## 8. Database design

Table `Donations`:

| Column | Type | Constraint | Reason |
| --- | --- | --- | --- |
| `Id` | `int` IDENTITY | Primary key | Auto-generated id |
| `Amount` | `decimal(18,2)` | NOT NULL, `CHECK ([Amount] > 0)` | Exact money type; DB rejects invalid amounts too |
| `DonorName` | `nvarchar(100)` | NOT NULL | Unicode for Arabic names |
| `IsAnonymous` | `bit` | DEFAULT 0 | Public by default |
| `CreatedAt` | `datetime2` | DEFAULT `SYSUTCDATETIME()` | Stored and returned in UTC |
| — | Index `IX_Donations_CreatedAt` | | Fast newest-first listing |

Validation is layered: the DTO (`[Range]`, `[Required]`, `[StringLength]`) gives the client a clear 400, and the `CHECK` constraint protects the data even if it is written from outside the API.
