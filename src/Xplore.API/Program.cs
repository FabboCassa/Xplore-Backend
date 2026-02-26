using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Xplore.Application;
using Xplore.Infrastructure;
using Xplore.Infrastructure.Identity;
using Xplore.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// --- Aspire ServiceDefaults (OpenTelemetry, Health Checks, Service Discovery) ---
builder.AddServiceDefaults();

// --- Database (PostgreSQL via Aspire) ---
builder.AddNpgsqlDbContext<ApplicationDbContext>("xploredb");

// --- ASP.NET Identity ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 4;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    // User settings
    options.User.RequireUniqueEmail = true;

    // Account Lockout — max 5 tentativi, blocco 15 minuti
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// --- JWT Authentication ---
var jwtKey = builder.Configuration["Jwt:Key"] ?? "xplore-dev-key-for-development-only-32chars";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Xplore";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "XploreApp";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero // No tolerance for expiry
    };
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Auth:Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Auth:Google:ClientSecret"]!;
})
.AddApple(options =>
{
    options.ClientId = builder.Configuration["Auth:Apple:ServiceId"]!;
    options.TeamId = builder.Configuration["Auth:Apple:TeamId"]!;
    options.KeyId = builder.Configuration["Auth:Apple:KeyId"]!;
    options.GenerateClientSecret = true;
});

builder.Services.AddAuthorization();

// --- Vector Database (Qdrant via Aspire) ---
builder.AddQdrantClient("vectordb");

// --- Application Layer (MediatR, Validators) ---
builder.Services.AddApplicationServices();

// --- Infrastructure Layer (Semantic Kernel, AI Services) ---
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

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

// --- Apply EF Core Migrations and Seed Roles in Development ---
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    
    // Seed roles
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = ["Admin", "MuseumManager", "Visitor"];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
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
app.UseAuthentication(); // MUST come before UseAuthorization
app.UseAuthorization();
app.MapControllers();

app.Run();