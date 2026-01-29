# Xplore.Application

## Descrizione
Libreria che contiene la **logica applicativa** e i casi d'uso del sistema. Implementa il pattern CQRS (Command Query Responsibility Segregation) e Mediator.

## Responsabilità
- Definizione dei casi d'uso (Use Cases)
- Orchestrazione della logica di business
- Validazione degli input
- Mappatura tra DTOs e entità di dominio

## Struttura
```
Xplore.Application/
├── Class1.cs                    # Placeholder
└── Xplore.Application.csproj
```

## Dipendenze
- **Xplore.Domain** - Entità di dominio
- **Xplore.Contracts** - Contratti di messaggistica

## Pattern Architetturali
- **CQRS**: Separazione tra comandi (write) e query (read)
- **Mediator**: Disaccoppiamento tra controller e handler

## Estensioni Future
- Commands e CommandHandlers
- Queries e QueryHandlers
- Validators con FluentValidation
- Mapping profiles con AutoMapper

## Target Framework
- .NET 10.0
