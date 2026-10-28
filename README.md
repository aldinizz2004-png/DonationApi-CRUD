# Donation Management API

ProGineer Backend Technical Task 5 (CRUD) · **Izzaldeen** · ASP.NET Core (.NET 10) · EF Core 10 · SQL Server

A RESTful API to create and manage donations. **If a donation is anonymous, the donor's name is never exposed in any API response.**

**Results:** 6 endpoints · **keyset (cursor) pagination** on the list · 27 Postman requests · **67 / 67 tests passed**

Documents: `DonationApi_Task5_Presentation.pdf` (CRUD walkthrough) · `Pagination_Report.pdf` (pagination research and decision)

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
│   ├── DonationResponse.cs      # What the client receives (name masked if anonymous)
│   ├── DonationPageQuery.cs     # GET list query: pageSize, cursor, sort, from, to, minAmount, includeTotal
│   ├── DonationCursor.cs        # Opaque Base64Url cursor = (CreatedAt, Id, sort)
│   └── CursorPage.cs            # Page response: items, pageSize, hasNextPage, nextCursor, totalCount
├── Models/
│   └── Donation.cs              # Database entity
├── Services/
│   ├── IDonationService.cs
│   └── DonationService.cs       # Business logic + the single entity -> response mapping
├── Data/
│   └── AppDbContext.cs          # Table design: types, constraints, index
├── Migrations/                  # InitialCreate, KeysetPaginationIndex
├── Postman/
│   └── DonationApi.postman_collection.json
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
| GET | `/api/donations` | Donations, **paged** (cursor), newest or oldest first, optional filters (anonymous names masked) | 200 · 400 |
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

## 6. Pagination (keyset / cursor)

`GET /api/donations` returns one page at a time. Instead of "skip N rows" it remembers **the last donation the client saw** (`CreatedAt`, `Id`) in an opaque `cursor` and continues strictly after it.

| Query parameter | Default | Rule |
| --- | --- | --- |
| `pageSize` | 20 | 1 to 100 |
| `cursor` | none (first page) | the `nextCursor` from the previous response |
| `sort` | `newest` | `newest` or `oldest` |
| `from`, `to` | none | UTC date-time range on `CreatedAt` (`from` ≤ `to`) |
| `minAmount` | none | > 0 |
| `includeTotal` | `false` | `true` adds `totalCount` (one extra `COUNT` query) |

```
GET /api/donations?pageSize=2&sort=oldest
```

```json
{
  "items": [
    { "id": 9,  "amount": 10.00, "donorName": "PagingTest 1", "isAnonymous": false, "createdAt": "2026-10-01T01:00:00Z" },
    { "id": 10, "amount": 20.00, "donorName": "PagingTest 2", "isAnonymous": false, "createdAt": "2026-10-01T01:00:00Z" }
  ],
  "pageSize": 2,
  "hasNextPage": true,
  "nextCursor": "eyJ0Ijo2MzkyNjQxMzIwMDAwMDAwMDAsImkiOjEwLCJzIjoxfQ",
  "totalCount": null
}
```

Next page: `GET /api/donations?pageSize=2&sort=oldest&cursor=eyJ0Ijo2Mzky...`. When `hasNextPage` is `false`, `nextCursor` is `null`.

SQL generated by EF Core for a next page (one query, only the needed columns, the name is masked inside SQL):

```sql
SELECT TOP(@p) [d].[Id], [d].[Amount],
       CASE WHEN [d].[IsAnonymous] = CAST(1 AS bit) THEN N'Anonymous' ELSE [d].[DonorName] END AS [DonorName],
       [d].[IsAnonymous], [d].[CreatedAt]
FROM [Donations] AS [d]
WHERE [d].[CreatedAt] < @lastCreatedAt OR ([d].[CreatedAt] = @lastCreatedAt AND [d].[Id] < @lastId)
ORDER BY [d].[CreatedAt] DESC, [d].[Id] DESC
```

**Why keyset and not `Skip/Take`:** measured on 1,000,000 rows, the last page took 567.6 ms with OFFSET (it reads and discards ~1M index rows) and 0.2 ms with keyset (an index seek reads 20 rows). When 3 donations were inserted between page 1 and page 2, OFFSET returned 3 duplicates and keyset returned 0. Details in `Pagination_Report.pdf`.

**How it is built:**
- `Take(pageSize + 1)`: the extra row tells us if a next page exists, without a `COUNT`.
- `Id` is the tie-breaker in both the `ORDER BY` and the cursor, so donations with the same `CreatedAt` are never skipped or repeated.
- Index `IX_Donations_CreatedAt_Id (CreatedAt DESC, Id DESC) INCLUDE (Amount, DonorName, IsAnonymous)` matches the order, the cursor seek and the date filter, and covers every column of the page.
- The cursor is validated: a broken cursor, or one created for another `sort`, returns 400.
- Works on any SQL Server version (`TOP` + `WHERE`); OFFSET/FETCH would need SQL Server 2012+.

## 7. Postman testing

1. Open Postman (desktop app, or web with the **Postman Desktop Agent** so it can reach `localhost`).
2. **Import** `Postman/DonationApi.postman_collection.json`.
3. Make sure the API is running. The collection variable `baseUrl` is `http://localhost:5207`.
4. Open the collection → **Run** → **Start run**.

Expected result: **27 requests, 67 tests passed, 0 failed.**

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
| Pagination | First page → next page with cursor (a new donation is inserted in between) → oldest + filters + total | 200, no duplicates, correct order, masked names |
| Pagination errors | pageSize 0 / 101 · invalid cursor · from after to · unknown sort | 400 with field errors |

The **Pagination** folder creates its own test donations and deletes them in `Paging - Cleanup`.

## 8. Key design decisions

**Anonymity enforced in one place.** `DonationService.ToResponseExpression` is the only code that turns a `Donation` entity into a `DonationResponse`, and every endpoint returns through it: the list runs it inside SQL, single items use its compiled form (`ToResponse`). When `IsAnonymous` is true it returns `"Anonymous"` instead of the name, so no endpoint can leak it. The real name is still stored, so an admin keeps the record and the name reappears if the donation is later made public.

**DTOs instead of the entity.** Requests can't set `Id` or `CreatedAt`. Request fields are nullable (`decimal?`, `bool?`) with `[Required]`, so a missing `amount` or `isAnonymous` returns 400 instead of silently becoming `0` / `false`.

**Thin controller, service layer, no repository.** Controllers only map results to HTTP status codes; the service holds the logic. `DbContext` already acts as a unit of work and repository, so an extra repository layer would be over-engineering.

**Consistent errors.** All errors use RFC 7807 `ProblemDetails` (`AddProblemDetails`, `UseExceptionHandler`, `UseStatusCodePages`). Unhandled exceptions return 500 without leaking a stack trace.

**Id validation.** The route is `{id}`, not `{id:int}`, so `/api/donations/abc` returns **400** (bad input) instead of **404**. Ids ≤ 0 also return 400.

**Health check includes the database.** `GET /api/health` returns 200 only when the database is reachable, otherwise 503.

## 9. Database design

Table `Donations`:

| Column | Type | Constraint | Reason |
| --- | --- | --- | --- |
| `Id` | `int` IDENTITY | Primary key | Auto-generated id |
| `Amount` | `decimal(18,2)` | NOT NULL, `CHECK ([Amount] > 0)` | Exact money type; DB rejects invalid amounts too |
| `DonorName` | `nvarchar(100)` | NOT NULL | Unicode for Arabic names |
| `IsAnonymous` | `bit` | DEFAULT 0 | Public by default |
| `CreatedAt` | `datetime2` | DEFAULT `SYSUTCDATETIME()` | Stored and returned in UTC |
| — | Index `IX_Donations_CreatedAt_Id` (`CreatedAt DESC, Id DESC`, INCLUDE `Amount, DonorName, IsAnonymous`) | | Keyset pagination: seek + order + covering |

Validation is layered: the DTO (`[Range]`, `[Required]`, `[StringLength]`) gives the client a clear 400, and the `CHECK` constraint protects the data even if it is written from outside the API.
