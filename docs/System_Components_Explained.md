# 🎓 Xplore: Guida ai Componenti del Sistema

Questa guida spiega **cosa** è ogni pezzo del sistema, **perché** lo usiamo e **dove** trovarlo nel codice.

---

## 1. RabbitMQ (Il Postino) 🐇

### Cos'è?
È un **Message Broker**. Immaginalo come un ufficio postale centrale.
Invece di mandare le lettere direttamente a casa del destinatario (che potrebbe non essere a casa), le porti all'ufficio postale. Lui le tiene al sicuro finché il postino (Worker) non è libero di consegnarle.

### Perché ci serve?
In Xplore, quando carichi un PDF pesante:
- **SENZA RabbitMQ**: L'utente deve aspettare con la clessidra che il PDF venga letto (lento!). Se il server cade, il PDF è perso.
- **CON RabbitMQ**: L'API dice "Ok, ho preso il file" in 1 millisecondo e mette un messaggio nella coda (`DocumentUploaded`). L'utente è libero subito.
- Il **Worker** (che lavora dietro le quinte) prende il messaggio con calma e processa il file.

### Dove lo trovi nel codice?
- **Configurazione**: `src/Xplore.AppHost/Program.cs` (Container Docker)
- **Evento (Lettera)**: `src/Xplore.Contracts/DocumentUploadedEvent.cs`
- **Mittente**: `src/Xplore.API/Controllers/DocumentsController.cs` (`_publishEndpoint.Publish(...)`)
- **Destinatario**: `src/Xplore.Worker/Consumers/DocumentUploadedConsumer.cs`

---

## 2. Qdrant (Il Cervello Vettoriale) 🧠

### Cos'è?
È un **Vector Database**. Un database normale (SQL) cerca per parole esatte ("gatto" trova "gatto").
Un database vettoriale capisce il **significato**. Trasforma il testo in numeri (vettori).
Esempio: "Gatto" e "Felino" sono parole diverse, ma numericamente vicine nello spazio vettoriale.

### Perché ci serve?
Per la **RAG (Retrieval-Augmented Generation)**.
Quando chiedi "Chi è l'architetto?", Qdrant cerca tra i mille pezzi di testo caricati (Chunks) quello che *significa* qualcosa di simile all'architetto, anche se non usa le stesse parole. Senza Qdrant, l'AI (ChatGPT) non saprebbe nulla dei tuoi documenti privati.

### Dove lo trovi nel codice?
- **Configurazione**: `src/Xplore.AppHost/Program.cs`
- **Salvataggio**: `src/Xplore.Worker/Consumers/DocumentUploadedConsumer.cs` (quando processiamo il PDF)
- **Ricerca**: `src/Xplore.Infrastructure/AI/QdrantVectorSearchService.cs` (quando chattiamo)

---

## 3. Worker (L'Operaio) 👷

### Cos'è?
È un programma che gira in background, senza interfaccia grafica. Non risponde agli utenti, ma lavora sodo.

### Perché ci serve?
Per fare i lavori pesanti senza rallentare il sito web (API).
Lavora:
1. Leggere i PDF (pesante!)
2. Generare Embeddings (lento perché chiama OpenAI)
3. Analizzare il Sentiment (futuro)

### Dove lo trovi nel codice?
- Progetto: `src/Xplore.Worker`

---

## 4. Semantic Kernel (Il Direttore d'Orchestra AI) 🤖

### Cos'è?
È una libreria di Microsoft che ci aiuta a parlare con le AI (OpenAI, Mistral, Llama) in modo strutturato. È come un traduttore universale.

### Perché ci serve?
Invece di scrivere codice grezzo per chiamare le API di OpenAI, usiamo Semantic Kernel che ci dà:
- **Memoria** (per ricordarsi la conversazione)
- **Astrazione** (se domani vuoi cambiare OpenAI con Google Gemini, cambi 1 riga)
- **Plugin** (capacità di dare "mani" all'AI per fare cose)

### Dove lo trovi nel codice?
- Configurazione: `src/Xplore.Infrastructure/DependencyInjection.cs`
- Utilizzo: `src/Xplore.Infrastructure/AI/SemanticKernelTextGenerationService.cs`

---

## 🧪 Come Testare Ora (Modalità Mock)

Ora che hai finito il credito OpenAI, abbiamo attivato la **Modalità Mock**.
I servizi fingono di lavorare: non chiamano OpenAI, non spendono soldi, ma testano tutto il resto (PDF, Code, Database).

### Step 1: Attiva i Mock (Opzionale se non hai la Key)
Se vuoi essere sicuro di usare i mock:
```powershell
dotnet user-secrets set "AI:UseMock" "true" --project src/Xplore.API
dotnet user-secrets set "AI:UseMock" "true" --project src/Xplore.Worker
```

### Step 2: Avvia
```powershell
dotnet run --project src/Xplore.AppHost
```

### Step 3: Carica il PDF (Triggera RabbitMQ + Worker + Qdrant)
Dal terminale (o da Postman):
```bash
curl -v -X POST "https://localhost:7234/api/documents/11111111-1111-1111-1111-111111111111" -F "file=@mioguida.pdf"
```
*(Controlla la porta esatta 7xxx nella dashboard Aspire all'avvio)*

**Cosa dovresti vedere:**
- L'API risponde subito `202 Accepted`.
- Nel terminale del Worker vedrai scorrere i log:
  - "📄 Document received..." (RabbitMQ ha consegnato!)
  - "Extracted 5000 chars..." (PdfPig ha letto!)
  - "🧪 [MOCK] Generating fake embedding..." (Il mock ha lavorato!)
  - "✅ Document processed!" (Salvato su Qdrant!)

### Step 4: Chatta (Triggera Qdrant + Semantic Kernel)
```bash
curl -v -X POST "https://localhost:7234/api/chat" -H "Content-Type: application/json" -d "{ \"question\": \"Cosa dice il documento?\" }"
```

**Cosa dovresti vedere:**
- Risposta JSON con testo: "🧪 [RISPOSTA MOCK - Modalità Sviluppo]..."
- Questo conferma che l'API ha interrogato Qdrant e assemblato la risposta.

Se vedi questo, **hai costruito un sistema RAG distribuito funzionante!** 🚀
