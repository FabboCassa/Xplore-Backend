using MassTransit;
using Xplore.Infrastructure;
using Xplore.Infrastructure.Persistence;
using Xplore.Worker.Consumers;
using Xplore.Worker.Jobs;

var builder = Host.CreateApplicationBuilder(args);

// --- Aspire ServiceDefaults (OpenTelemetry, Health Checks, Service Discovery) ---
builder.AddServiceDefaults();

// --- Database (PostgreSQL via Aspire) ---
builder.AddNpgsqlDbContext<ApplicationDbContext>("xploredb");

// --- Vector Database (Qdrant via Aspire) ---
builder.AddQdrantClient("vectordb");

// --- Infrastructure Layer (Semantic Kernel, AI Services) ---
builder.Services.AddInfrastructureServices(builder.Configuration);

// --- MassTransit with RabbitMQ (uses Aspire connection string) ---
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<MuseumCreatedConsumer>();
    x.AddConsumer<DocumentUploadedConsumer>();
    x.AddConsumer<InteractionCreatedConsumer>();
    x.AddConsumer<FriendRequestNotificationConsumer>();
    x.AddConsumer<AchievementNotificationConsumer>();
    x.AddConsumer<GroupInviteNotificationConsumer>();
    x.AddConsumer<CompetitionEndedNotificationConsumer>();

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
        cfg.ConfigureEndpoints(context);
    });
});

// --- Background Jobs ---
builder.Services.AddHostedService<CompetitionExpiryJob>();

var host = builder.Build();
host.Run();
