# Xplore.Domain

## Descrizione
Libreria di classi che contiene le **entità di dominio** dell'applicazione. Rappresenta il cuore del sistema secondo i principi della Clean Architecture.

## Responsabilità
- Definizione delle entità di business
- Regole di dominio e validazioni
- Nessuna dipendenza da framework esterni

## Struttura
```
Xplore.Domain/
├── Entities/
│   └── Museum.cs      # Entità Museo
└── Xplore.Domain.csproj
```

## Entità

### Museum
Rappresenta un ente museale nel sistema.

| Proprietà | Tipo | Descrizione |
|-----------|------|-------------|
| Id | Guid | Identificativo univoco |
| Name | string | Nome del museo |
| Description | string | Descrizione |
| CreatedAt | DateTime | Data di creazione |

## Dipendenze
Nessuna dipendenza esterna - progetto puro .NET.

## Target Framework
- .NET 10.0
