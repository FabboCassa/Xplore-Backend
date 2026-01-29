# Xplore.API

## Descrizione
Web API REST che funge da **punto di ingresso** per il sistema. Gestisce le richieste HTTP degli utenti e orchestra le operazioni di business.

## Responsabilità
- Esporre endpoint REST
- Autenticazione e autorizzazione
- Validazione delle richieste
- Pubblicazione eventi su RabbitMQ
- Risposta immediata agli utenti

## Struttura
```
Xplore.API/
├── Controllers/              # Controller REST
├── Properties/
│   └── launchSettings.json   # Configurazione avvio
├── Program.cs                # Entry point e configurazione
├── appsettings.json          # Configurazione applicativa
└── Xplore.API.csproj
```

## Configurazione

### Aspire Integration
```csharp
builder.AddServiceDefaults();           // OpenTelemetry, Health Checks
builder.AddNpgsqlDbContext<...>("xploredb");  // PostgreSQL via Aspire
```

### MassTransit
```csharp
builder.Services.AddMassTransit(x => {
    x.UsingRabbitMq((context, cfg) => {
        // Configurazione RabbitMQ
    });
});
```

## Endpoints

### Health Checks
- `GET /health` - Stato generale dell'applicazione
- `GET /alive` - Liveness check

### Swagger
- `GET /swagger` - Documentazione API (solo in Development)

## Dipendenze
- **Xplore.Application** - Logica applicativa
- **Xplore.Infrastructure** - Persistenza
- **Xplore.Contracts** - Eventi
- **Xplore.ServiceDefaults** - Configurazione Aspire
- **Aspire.Npgsql.EntityFrameworkCore.PostgreSQL** - Database
- **MassTransit.RabbitMQ** - Messaggistica

## Avvio
```bash
dotnet run --project src/Xplore.API
```

O tramite Aspire:
```bash
dotnet run --project src/Xplore.AppHost
```

## Target Framework
- .NET 10.0
