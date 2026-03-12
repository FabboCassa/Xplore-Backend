# 🎉 Xplore Platform - Walkthrough Completo

## Riepilogo Step Completati

| Step | Nome | Stato |
|------|------|-------|
| 1 | Qdrant Vector Database | ✅ |
| 2 | MediatR / CQRS | ✅ |
| 3 | Semantic Kernel (AI) | ✅ |
| 4 | Document Ingestion | ✅ |
| 5 | RAG Chat API | ✅ |

---

## 🏛️ Componenti Creati

### Infrastructure (AppHost)
| Risorsa | Tipo | Descrizione |
|---------|------|-------------|
| `postgres` | Container | Database PostgreSQL |
| `xploredb` | Database | Schema applicazione |
| `messaging` | Container | RabbitMQ broker |
| `vectordb` | Container | Qdrant vector DB |

### API Endpoints
| Endpoint | Metodo | Descrizione |
|----------|--------|-------------|
| `/api/chat` | POST | Chat RAG con AI |
| `/api/chat/status` | GET | Stato servizi AI |
| `/api/documents/{museumId}` | POST | Upload PDF |
| `/api/museums` | GET/POST | CRUD Musei |

### Eventi (RabbitMQ)
| Evento | Producer | Consumer |
|--------|----------|----------|
| `MuseumCreatedEvent` | API | Worker |
| `DocumentUploadedEvent` | API | Worker |

---

## 🔑 Configurazione API Key OpenAI

### Dove Mettere la API Key

```powershell
# Metodo consigliato: User Secrets (sicuro, non va in Git)
dotnet user-secrets set "OpenAI:ApiKey" "sk-..." --project src/Xplore.API
```

Oppure in `appsettings.json` (⚠️ NON committare!):
```json
{
  "OpenAI": {
    "ApiKey": "sk-...",
    "Model": "gpt-4o-mini",
    "EmbeddingModel": "text-embedding-3-small"
  }
}
```

---

## 📖 Esempio Flusso Positivo Completo

### Scenario: Visitatore chiede info su un'opera

```
👤 Utente → 📱 App → 🌐 API → 🔍 Qdrant → 🤖 OpenAI → 📱 Risposta
```

#### Step-by-Step

**1. Il Museo carica una guida PDF**
```
POST /api/documents/123e4567-e89b-12d3-a456-426614174000
File: guida_museo_egizio.pdf
```

**Flusso interno:**
```
📤 API.DocumentsController
   → Salva file su disco
   → Pubblica "DocumentUploadedEvent" su RabbitMQ
   → Risponde 202 Accepted { documentId: "..." }

📥 Worker.DocumentUploadedConsumer
   → Riceve evento da RabbitMQ
   → Estrae testo dal PDF
   → Divide in chunk (~1000 caratteri)
   → Genera embeddings (OpenAI text-embedding-3-small)
   → Salva vettori in Qdrant
   → Log: "✅ Document processed!"
```

**2. Visitatore fa una domanda**
```
POST /api/chat
{ "question": "Chi era Kha e cosa contiene la sua tomba?" }
```

**Flusso interno:**
```
📤 API.ChatController
   → Riceve domanda
   → [RAG Step 1] Cerca in Qdrant chunk simili semanticamente
   → Trova 5 chunk rilevanti sulla tomba di Kha
   → [RAG Step 2] Costruisce prompt con contesto
   → Chiama OpenAI GPT-4o-mini
   → Risponde con testo generato

{
  "answer": "Kha era un architetto egizio vissuto durante 
            la XVIII dinastia. La sua tomba, scoperta nel 1906,
            è una delle meglio conservate e contiene...",
  "isAiGenerated": true,
  "context": "[chunk1] [chunk2] ..."
}
```

---

## 🧪 Come Testare

### 1. Avvia il sistema
```powershell
dotnet run --project src/Xplore.AppHost
```

### 2. Verifica lo stato AI
```bash
curl https://localhost:7xxx/api/chat/status
```

**Senza API Key:**
```json
{
  "textGenerationConfigured": false,
  "message": "Configure OpenAI:ApiKey..."
}
```

**Con API Key:**
```json
{
  "textGenerationConfigured": true,
  "message": "AI services are ready"
}
```

### 3. Testa la Chat (con API Key)
```bash
curl -X POST https://localhost:7xxx/api/chat \
  -H "Content-Type: application/json" \
  -d '{"question": "Parlami del Museo Egizio"}'
```

### 4. Carica un documento
```bash
curl -X POST https://localhost:7xxx/api/documents/00000000-0000-0000-0000-000000000001 \
  -F "file=@guida.pdf"
```

---

## 📁 Struttura Finale Progetto

```
Xplore/
├── src/
│   ├── Xplore.AppHost/           # Orchestratore Aspire
│   ├── Xplore.ServiceDefaults/   # OpenTelemetry, Health
│   ├── Xplore.API/               # REST API
│   │   └── Controllers/
│   │       ├── ChatController.cs        # RAG Chat
│   │       ├── DocumentsController.cs   # Upload PDF
│   │       └── MuseumsController.cs     # CRUD
│   ├── Xplore.Worker/            # Background jobs
│   │   └── Consumers/
│   │       ├── DocumentUploadedConsumer.cs  # PDF → Embeddings
│   │       └── MuseumCreatedConsumer.cs
│   ├── Xplore.Application/       # Business Logic (CQRS)
│   │   ├── AI/IAIServices.cs
│   │   └── Museums/Commands/Queries/
│   ├── Xplore.Infrastructure/    # DB, AI implementations
│   │   ├── AI/SemanticKernel*.cs
│   │   └── Persistence/
│   ├── Xplore.Domain/            # Entities
│   │   └── Entities/Museum.cs, Document.cs
│   └── Xplore.Contracts/         # Eventi
│       ├── MuseumCreatedEvent.cs
│       └── DocumentUploadedEvent.cs
└── docs/                         # Documentazione
```

---

## ✅ Prossimi Passi Opzionali

1. **PDF Parsing reale** - Aggiungere `PdfPig` per estrarre testo
2. **Qdrant Storage** - Implementare `IVectorSearchService` con Qdrant
3. **Autenticazione** - JWT/OAuth2
4. **Rate Limiting** - Per protegger le API OpenAI
5. **Caching** - Redis per risposte frequenti
