# Xplore.Infrastructure

## Descrizione
Libreria che contiene l'**implementazione dei servizi infrastrutturali**: persistenza, messaggistica, servizi esterni.

## Responsabilità
- Configurazione Entity Framework Core
- Implementazione dei repository
- Configurazione PostgreSQL
- Migrazioni database

## Struttura
```
Xplore.Infrastructure/
├── Persistence/
│   ├── ApplicationDbContext.cs   # DbContext principale
│   └── Migrations/               # Migrazioni EF Core
└── Xplore.Infrastructure.csproj
```

## Database Context

### ApplicationDbContext
Contesto principale per l'accesso al database PostgreSQL.

```csharp
public class ApplicationDbContext : DbContext
{
    public DbSet<Museum> Museums { get; set; }
}
```

## Dipendenze
- **Xplore.Application** - Interfacce da implementare
- **Xplore.Domain** - Entità di dominio
- **Npgsql.EntityFrameworkCore.PostgreSQL** - Provider PostgreSQL
- **MassTransit.RabbitMQ** - Messaggistica
- **RabbitMQ.Client** - Client RabbitMQ

## Configurazione Database
La connection string viene gestita tramite .NET Aspire con il nome `xploredb`.

## Migrazioni
```bash
# Creare una nuova migrazione
dotnet ef migrations add NomeMigrazione -p src/Xplore.Infrastructure -s src/Xplore.API

# Applicare le migrazioni
dotnet ef database update -p src/Xplore.Infrastructure -s src/Xplore.API
```

## Target Framework
- .NET 10.0
