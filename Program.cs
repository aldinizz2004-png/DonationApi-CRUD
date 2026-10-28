using DonationApi.Data;
using DonationApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ===================== Services =====================

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        // enums as text in JSON and Swagger ("newest" / "oldest") instead of 0 / 1
        o.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase)));

// Database (SQL Server)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Business logic
builder.Services.AddScoped<IDonationService, DonationService>();

// Standard error format (RFC 7807 ProblemDetails) for 400 / 404 / 500
builder.Services.AddProblemDetails();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Donation Management API",
        Version = "v1",
        Description = "CRUD API for donations. Anonymous donors' names are never exposed."
    });
});

var app = builder.Build();

// ===================== Database migrations =====================
// In Docker the database starts empty, so the API creates/updates the schema on startup.
// Enabled by Database__MigrateOnStartup=true (set in compose.yaml); off for normal local runs.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await MigrateDatabaseAsync(app);
}

// ===================== Pipeline =====================

// Any unhandled exception -> 500 ProblemDetails (no stack trace leaked to the client)
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();   // http://localhost:<port>/swagger
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

// Retries because SQL Server may still be starting when the API container starts.
static async Task MigrateDatabaseAsync(WebApplication app)
{
    const int maxAttempts = 10;
    var logger = app.Logger;

    for (var attempt = 1; ; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied.");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning("Database not ready (attempt {Attempt}/{Max}): {Message}. Retrying in 5 s...",
                attempt, maxAttempts, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}