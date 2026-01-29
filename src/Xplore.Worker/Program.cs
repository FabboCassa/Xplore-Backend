using MassTransit;
using Xplore.Worker.Consumers;

var builder = Host.CreateApplicationBuilder(args);

// --- Aspire ServiceDefaults (OpenTelemetry, Health Checks, Service Discovery) ---
builder.AddServiceDefaults();

// --- MassTransit with RabbitMQ (uses Aspire connection string) ---
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<MuseumCreatedConsumer>();

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

var host = builder.Build();
host.Run();
