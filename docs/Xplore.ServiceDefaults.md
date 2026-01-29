# Xplore.ServiceDefaults

## Descrizione
Libreria condivisa che fornisce **configurazioni standard** per tutti i servizi Aspire: telemetria, health checks, resilienza e service discovery.

## Responsabilità
- Configurazione OpenTelemetry (tracing, metrics, logging)
- Health checks standardizzati
- Service discovery automatico
- Pattern di resilienza HTTP
- Esportazione telemetria verso Aspire Dashboard

## Struttura
```
Xplore.ServiceDefaults/
├── Extensions.cs                    # Metodi di estensione
└── Xplore.ServiceDefaults.csproj
```

## API Principale

### AddServiceDefaults
Configura tutti i servizi standard per un'applicazione Aspire.

```csharp
builder.AddServiceDefaults();
```

Include:
- OpenTelemetry (tracing + metrics)
- Health checks (`/health`, `/alive`)
- Service discovery
- HTTP resilience handlers

### MapDefaultEndpoints
Registra gli endpoint di health check.

```csharp
app.MapDefaultEndpoints();
```

## Health Checks

| Endpoint | Descrizione |
|----------|-------------|
| `/health` | Tutti i check devono passare |
| `/alive` | Solo liveness check (app responsiva) |

## OpenTelemetry

### Tracing
- ASP.NET Core instrumentation
- HTTP client instrumentation
- Source personalizzata per app

### Metrics
- Runtime metrics (.NET)
- ASP.NET Core metrics
- HTTP client metrics

## Dipendenze
- **Microsoft.Extensions.Http.Resilience** - Resilienza HTTP
- **Microsoft.Extensions.ServiceDiscovery** - Service discovery
- **OpenTelemetry.*** - Suite OpenTelemetry

## Utilizzo
Questo progetto viene referenziato da `Xplore.API` e `Xplore.Worker`.

```xml
<ProjectReference Include="..\Xplore.ServiceDefaults\..." />
```

## Target Framework
- .NET 10.0
