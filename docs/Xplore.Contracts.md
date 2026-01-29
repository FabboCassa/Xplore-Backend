# Xplore.Contracts

## Descrizione
Libreria condivisa che contiene i **contratti di messaggistica** (eventi e comandi) utilizzati per la comunicazione asincrona tra i servizi.

## Responsabilità
- Definizione degli eventi di dominio
- Contratti condivisi tra API e Worker
- DTOs per la messaggistica

## Struttura
```
Xplore.Contracts/
├── MuseumCreatedEvent.cs    # Evento pubblicato quando viene creato un museo
└── Xplore.Contracts.csproj
```

## Eventi

### MuseumCreatedEvent
Evento pubblicato quando un nuovo museo viene creato nel sistema.

```csharp
public record MuseumCreatedEvent(Guid MuseumId, string Name);
```

| Proprietà | Tipo | Descrizione |
|-----------|------|-------------|
| MuseumId | Guid | ID del museo creato |
| Name | string | Nome del museo |

## Dipendenze
- **MassTransit** - Framework per la messaggistica

## Utilizzo
Questo progetto viene referenziato sia da `Xplore.API` (producer) che da `Xplore.Worker` (consumer).

## Target Framework
- .NET 10.0
