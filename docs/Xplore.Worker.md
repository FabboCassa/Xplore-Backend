# Xplore.Worker

## Descrizione
Servizio in background (Worker Service) che **elabora eventi** pubblicati dall'API tramite RabbitMQ.

## Responsabilità
- Consumare eventi dalla coda RabbitMQ
- Elaborare task asincroni pesanti
- Aggiornare database analytics
- Processare documenti (PDF to Vector)

## Struttura
```
Xplore.Worker/
├── Consumers/
│   └── MuseumCreatedConsumer.cs   # Consumer per eventi museo
├── Program.cs                      # Entry point
├── Worker.cs                       # Background service base
├── appsettings.json               # Configurazione
└── Xplore.Worker.csproj
```

## Consumers

### MuseumCreatedConsumer
Gestisce gli eventi `MuseumCreatedEvent` pubblicati quando viene creato un nuovo museo.

```csharp
public class MuseumCreatedConsumer : IConsumer<MuseumCreatedEvent>
{
    public async Task Consume(ConsumeContext<MuseumCreatedEvent> context)
    {
        // Elaborazione asincrona
    }
}
```

## Configurazione

### Aspire Integration
```csharp
builder.AddServiceDefaults();  // OpenTelemetry, Health Checks
```

### MassTransit
```csharp
builder.Services.AddMassTransit(x => {
    x.AddConsumer<MuseumCreatedConsumer>();
    x.UsingRabbitMq((context, cfg) => {
        cfg.ConfigureEndpoints(context);
    });
});
```

## Dipendenze
- **Xplore.Application** - Logica applicativa
- **Xplore.Infrastructure** - Persistenza
- **Xplore.Contracts** - Definizione eventi
- **Xplore.ServiceDefaults** - Configurazione Aspire
- **MassTransit.RabbitMQ** - Messaggistica

## Avvio
```bash
dotnet run --project src/Xplore.Worker
```

O tramite Aspire:
```bash
dotnet run --project src/Xplore.AppHost
```

## Target Framework
- .NET 10.0
