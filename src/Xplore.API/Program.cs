using MassTransit;
using Microsoft.EntityFrameworkCore;
using Xplore.Application;
using Xplore.Infrastructure;
using Xplore.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// --- Aspire ServiceDefaults (OpenTelemetry, Health Checks, Service Discovery) ---
builder.AddServiceDefaults();

// --- Database (PostgreSQL via Aspire) ---
builder.AddNpgsqlDbContext<ApplicationDbContext>("xploredb");

// --- Vector Database (Qdrant via Aspire) ---
builder.AddQdrantClient("vectordb");

// --- Application Layer (MediatR, Validators) ---
builder.Services.AddApplicationServices();

// --- Infrastructure Layer (Semantic Kernel, AI Services) ---
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- MassTransit with RabbitMQ (uses Aspire connection string) ---
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("messaging");
        if (!string.IsNullOrEmpty(connectionString))
        {
            cfg.Host(new Uri(connectionString));
        }
        else
        {
            cfg.Host("localhost", "/", h =>
            {
                h.Username("guest");
                h.Password("guest");
            });
        }
    });
});

var app = builder.Build();

// --- Apply EF Core Migrations in Development ---
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

// --- Aspire default endpoints (health checks) ---
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();