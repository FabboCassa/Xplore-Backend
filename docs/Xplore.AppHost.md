# Xplore.AppHost

## Descrizione
Progetto **orchestratore .NET Aspire** che coordina l'avvio di tutti i servizi e le dipendenze infrastrutturali.

## Responsabilità
- Orchestrazione di tutti i servizi
- Gestione container Docker (PostgreSQL, RabbitMQ)
- Dashboard di monitoraggio integrata
- Service discovery automatico
- Distribuzione delle connection string

## Struttura
```
Xplore.AppHost/
├── Properties/
│   └── launchSettings.json    # Configurazione dashboard
├── Program.cs                 # Definizione risorse
└── Xplore.AppHost.csproj
```

## Risorse Orchestrate

### Infrastructure
| Risorsa | Tipo | Nome Riferimento |
|---------|------|------------------|
| PostgreSQL | Container | `postgres` |
| Database | PostgreSQL DB | `xploredb` |
| RabbitMQ | Container | `messaging` |

### Servizi Applicativi
| Servizio | Tipo | Dipendenze |
|----------|------|------------|
| Xplore.API | Project | postgres, messaging |
| Xplore.Worker | Project | postgres, messaging |

## Configurazione

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .AddDatabase("xploredb");

var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

// Services
builder.AddProject<Projects.Xplore_API>("xplore-api")
    .WithReference(postgres)
    .WithReference(rabbitmq);

builder.AddProject<Projects.Xplore_Worker>("xplore-worker")
    .WithReference(postgres)
    .WithReference(rabbitmq);
```

## Dashboard
All'avvio, Aspire fornisce una dashboard web per:
- Visualizzare lo stato di tutti i servizi
- Consultare log centralizzati
- Tracciare richieste distribuite
- Monitorare metriche in tempo reale

**URL Dashboard**: `https://localhost:17281`

## Avvio
```bash
dotnet run --project src/Xplore.AppHost
```

## Dipendenze
- **Aspire.AppHost.Sdk** - SDK Aspire (v9.1.0)
- **Aspire.Hosting.AppHost** - Runtime hosting
- **Aspire.Hosting.PostgreSQL** - Supporto PostgreSQL
- **Aspire.Hosting.RabbitMQ** - Supporto RabbitMQ

## Target Framework
- .NET 10.0
