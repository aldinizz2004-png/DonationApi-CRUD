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