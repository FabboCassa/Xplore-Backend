# 📘 Xplore: Architectural Overview & Tech Stack

## 1. Visione del Progetto
Xplore è una piattaforma B2B2C Cloud-Native progettata per rivoluzionare l'esperienza culturale museale. Il sistema funge da ponte tra il visitatore e l'ente museale:

**Lato Utente (B2C):** Offre una guida AI conversazionale (Chatbot RAG) capace di rispondere a domande complesse sulle opere in tempo reale, superando i limiti delle audioguide statiche.

**Lato Business (B2B):** Trasforma le interazioni degli utenti in dati analitici preziosi (interessi, flussi, sentiment), permettendo ai musei di prendere decisioni data-driven.

## 2. Strategia Architetturale: Distributed Modular Monorepo
Per bilanciare la velocità di sviluppo con la necessità di scalabilità Enterprise, Xplore adotta un'architettura Ibrida Distribuita.

Non è un monolite (che non scalerebbe) né una foresta di microservizi puri (che aggiungerebbe complessità operativa prematura). Il codice risiede in un unico repository (Monorepo) per condividere logiche di dominio e contratti, ma il runtime è fisicamente separato in servizi distinti che scalano indipendentemente.

### I Componenti Core
**Xplore.API (Front-Office):**
- Gestisce le richieste HTTP degli utenti (REST).
- Responsabile dell'Autenticazione e della risposta immediata.
- Stateless: Può scalare orizzontalmente su n istanze dietro un Load Balancer.

**Xplore.Worker (Back-Office):**
- Servizio in background che elabora task pesanti in modo asincrono.
- Gestisce l'ingestione dei documenti (PDF to Vector), l'analisi dei dati e la persistenza dei log analitici.
- Disaccoppia il carico di lavoro dall'esperienza utente.

## 3. Tech Stack & Decision Records

### 🔹 Core Framework & Language
**Tecnologia:** .NET 10 (C#)

**Perché:** Garantisce performance elevate (grazie al JIT e AOT compilation), un ecosistema maturo per l'Enterprise e una gestione della memoria robusta.

**Design Pattern:** Clean Architecture (Domain, Application, Infrastructure, Presentation). Questo garantisce che la logica di business sia indipendente da database, framework esterni o UI, facilitando i test e le future migrazioni tecnologiche.

### 🔹 Orchestration & Cloud Native
**Tecnologia:** .NET Aspire & Docker

**Perché:** Aspire è il nuovo standard per l'orchestrazione di applicazioni distribuite in ambiente .NET. Elimina la complessità della configurazione manuale di porte e connection strings, offrendo una dashboard integrata per log, metriche e tracce distribuite (Observability out-of-the-box). Docker garantisce la portabilità degli ambienti da sviluppo a produzione.

### 🔹 Database & Persistenza
**Relazionale:** PostgreSQL (con Entity Framework Core)
- **Uso:** Gestione strutturata di Utenti, Musei, Opere e Relazioni.
- **Perché:** Database open-source standard di mercato, scalabile e ricco di funzionalità avanzate (JSONB).

**Vettoriale:** Qdrant
- **Uso:** Memorizzazione degli embeddings (rappresentazioni vettoriali) dei documenti museali per la RAG.
- **Perché:** Ottimizzato specificamente per la ricerca semantica ad alta velocità, essenziale per ridurre la latenza delle risposte AI.

### 🔹 Comunicazione Asincrona (Event-Driven)
**Tecnologia:** RabbitMQ (Message Broker) gestito tramite MassTransit.

**Perché:** Il disaccoppiamento è vitale. L'API non deve attendere che il database analitico risponda per servire l'utente.

**Pattern:** Fire-and-Forget. L'API pubblica un evento (MuseumCreated, UserAskedQuestion) e risponde subito al client. Il Worker consuma l'evento con i suoi tempi. MassTransit astrae la complessità del broker, permettendo un facile switch futuro (es. verso Azure Service Bus) senza riscrivere codice.

### 🔹 Artificial Intelligence & RAG
**Tecnologia:** Microsoft Semantic Kernel & OpenAI / Azure OpenAI

**Approccio:** Retrieval-Augmented Generation (RAG).

**Perché:** Non ci limitiamo a chiamare le API di GPT. Semantic Kernel permette di orchestrare "Agenti" che hanno memoria a lungo termine (tramite Qdrant) e possono eseguire azioni (Plugin). Questo riduce le allucinazioni dell'AI, vincolando le risposte ai soli dati forniti dal museo.

### 🔹 DevOps & Infrastructure as Code (Future Implementation)
**Tecnologia:** Terraform & GitHub Actions

**Perché:** "You build it, you run it". L'infrastruttura non viene configurata manualmente cliccando su portali Cloud, ma definita come codice (IaC). Questo garantisce riproducibilità, sicurezza e permette di creare ambienti effimeri di test automatici prima di ogni deploy.

## 4. Flusso dei Dati (Data Flow)

### Scenario A: L'Utente Chiede informazioni su un'opera
1. L'App Client invia la domanda a Xplore.API.
2. Xplore.API interroga Qdrant per trovare i frammenti di testo (Chunk) semanticamente rilevanti.
3. Xplore.API costruisce un prompt arricchito (RAG) e lo invia al LLM (Semantic Kernel).
4. L'AI genera la risposta e l'API la restituisce all'utente.
5. Parallelamente, l'API pubblica un evento InteractionCreated su RabbitMQ.
6. Xplore.Worker riceve l'evento, esegue l'analisi del sentiment e aggiorna le statistiche su PostgreSQL senza rallentare la chat.

### Scenario B: Il Museo Carica una Guida PDF
1. Il Museo carica il file via Xplore.API.
2. L'API salva il file grezzo e pubblica un evento DocumentUploaded su RabbitMQ.
3. Xplore.Worker intercetta l'evento.
4. Il Worker esegue il parsing del testo, lo divide in chunk, calcola gli embedding tramite OpenAI e li salva su Qdrant, rendendo la conoscenza immediatamente disponibile per la chat.

## 5. Obiettivi di Qualità (Non-Functional Requirements)
- **Scalabilità:** Il sistema deve supportare picchi di traffico turistico (es. weekend o festività) scalando automaticamente i container API.
- **Resilienza:** Se il servizio di Analytics (Worker) fallisce, la Chat (API) deve continuare a funzionare. I messaggi verranno riprocessati al riavvio del Worker.
- **Observability:** Ogni richiesta è tracciata end-to-end (OpenTelemetry) per identificare colli di bottiglia tra i microservizi.
