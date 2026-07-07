using Microsoft.EntityFrameworkCore;
using QueueIQ.Api.Hubs;
using QueueIQ.Api.Middleware;
using QueueIQ.Api.Services;
using QueueIQ.Data;
using QueueIQ.Shared.Interfaces;
using Microsoft.Extensions.ML;
using QueueIQ.Api.Models;
using Serilog;
using Serilog.Formatting.Compact;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(new CompactJsonFormatter(), "logs/api-log-.json", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// ─── Database ────────────────────────────────────────────────────────────────
// SQLite for local dev — swap to SQL Server for Azure deployment by changing
// the provider and connection string. EF Core abstraction makes this painless.
builder.Services.AddDbContext<QueueIQDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=QueueIQ.db"));

// ─── Services (Dependency Injection) ─────────────────────────────────────────
// Scoped lifetime = one instance per HTTP request, matches EF Core's DbContext lifetime
builder.Services.AddScoped<IQueueService, QueueService>();
builder.Services.AddScoped<IBusinessService, BusinessService>();
builder.Services.AddScoped<IQueueNotificationService, QueueNotificationService>();

// ─── Real-Time (SignalR) ─────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ─── ML.NET Predictions ──────────────────────────────────────────────────────
var modelsPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "models"));
builder.Services.AddPredictionEnginePool<QueueInput, WaitTimePrediction>()
    .FromFile(modelName: "WaitTimeModel", filePath: Path.Combine(modelsPath, "wait_time_model.zip"), watchForChanges: true);
builder.Services.AddPredictionEnginePool<QueueInput, NoShowPrediction>()
    .FromFile(modelName: "NoShowModel", filePath: Path.Combine(modelsPath, "no_show_model.zip"), watchForChanges: true);

builder.Services.AddScoped<IPredictionService, PredictionService>();

// ─── API Configuration ──────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize enums as strings (e.g., "Waiting" instead of 0) for API readability
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();

// ─── Rate Limiting ───────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("QueueJoinLimiter", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ─── CORS ────────────────────────────────────────────────────────────────────
// Allow Blazor Server (different port) to call the API during development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5199",
                "https://localhost:7104")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // Required for SignalR in Phase 2
    });
});

var app = builder.Build();

// ─── Middleware Pipeline ─────────────────────────────────────────────────────
// Order matters: exception handling wraps everything else
app.UseApiExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Add Swagger UI for easier testing
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "QueueIQ API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowBlazor");
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();
app.MapHub<QueueHub>("/hubs/queue");

// ─── Auto-create database on startup (dev only) ─────────────────────────────
// In production, you'd use `dotnet ef database update` or migration bundles
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<QueueIQDbContext>();
    await db.Database.EnsureCreatedAsync();
}

await app.RunAsync();
