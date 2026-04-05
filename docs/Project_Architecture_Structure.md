# 🏗️ Xplore: Struttura del Progetto e Livelli (Clean Architecture & Aspire)

Questa guida spiega in modo semplice e chiaro a cosa serve ogni singola cartella (progetto) all'interno di `src/`. 
L'architettura usa due concetti chiave:
1. **Clean Architecture**: Il codice vitale (Domain) non sa nulla del mondo esterno (Database, API), e le dipendenze vanno sempre dall'esterno verso l'interno.
2. **.NET Aspire**: Un sistema moderno per orchestrare e lanciare più servizi assieme (API, Worker, Database, Componenti esterni).

Se devi spiegare l'app a un esterno, puoi usare l'analogia di un **Ristorante**.

---

## 🍽️ 1. Il Nucleo e la Logica (Il Ristorante in sé)

Questi due progetti formano il lato "puro" dell'applicazione. Non sanno che esiste Internet, non sanno cos'è un Database o Firebase. Contengono solo le regole del gioco.

### 🟢 `Xplore.Domain` (Le Ricette e gli Ingredienti Base)
È il cuore assoluto del sistema. Non dipende da *nessun'altra* cartella.
- **Cosa fa:** Definisce "che cos'è" ogni cosa. Le entità del mondo reale tradotte in codice.
- **Cosa ci trovi:** Le classi principali (Entità) come `User`, `Museum`, `SavedRoute`, `Friendship`, `Group`, `Competition`. Trovi anche le eccezioni di base e i concetti fondamentali del business (Value Objects come le Coordinate).
- **Spiegazione per esterni:** Sono le regole fisiche e concettuali del nostro mondo. (es: "Un Utente deve avere una mail", "Un Gruppo deve avere membri", "Una Route deve avere dei punti").

### 🟡 `Xplore.Application` (Il Cuoco / Il Cervello)
Dipende solo da `Domain`. 
- **Cosa fa:** Contiene tutti i **Casi d'Uso** dell'app. È il "Cervello" che decide *cosa* fare.
- **Cosa ci trovi:** I *Command* (es: `CreateRouteCommand`, `CreateGroupCommand`), le *Query* (es: `GetFriendsQuery`) e i loro *Handler*. Trovi anche le **Interfacce** (es: `IPushNotificationService`, `IApplicationDbContext`). Notare: qui si dice *che serve* inviare una notifica, ma non c'è il codice che sa come farlo.
- **Spiegazione per esterni:** È il capo chef che prende l'ordine, guarda le regole del dominio, e comanda agli sguatteri (Infrastruttura) di prendere gli ingredienti e preparare il piatto.

---

## 🔌 2. Interazione col Mondo Esterno (I Camerieri e i Fornitori)

Questi progetti contengono codice "sporco" che tocca il mondo reale: HTTP, Database, API esterne, Firebase, RabbitMQ.

### 🔵 `Xplore.Infrastructure` (I Fornitori e gli Sguatteri)
Dipende da `Application` e `Domain`.
- **Cosa fa:** Implementa (dà corpo) alle interfacce definite in Application. Parla fisicamente coi sistemi esterni.
- **Cosa ci trovi:**
  - **Database (Persistence):** Il file `ApplicationDbContext` (Entity Framework) che salva i dati in SQL.
  - **Mappe/API (Map):** Il codice che parla con OpenStreetMap o Wikipedia (`WikipediaApiService`).
  - **Intelligenza Artificiale (AI):** Semantic Kernel, Qdrant e chiamate a OpenAI/LLM.
  - **Notifiche (Notifications):** Il codice vero e proprio che chiama Firebase (`FirebasePushNotificationService`).
- **Spiegazione per esterni:** Sono i magazzinieri, i camionisti e i servizi esterni. Sanno esattamente dove e come parlare con il database, come chimare Google o Wikipedia.

### 🔴 `Xplore.API` (I Camerieri e la Cassiera)
Dipende da `Application` e `Infrastructure` (per iniettarli all'avvio).
- **Cosa fa:** È la porta di ingresso di Xplore da Internet (dal telefono). Ascolta richieste HTTP, chiama il caso d'uso in Application, e restituisce la risposta JSON.
- **Cosa ci trovi:** I Controller (es: `FriendsController`, `MapController`). Il file `Program.cs` che configura l'avvio della REST API. L'autenticazione JWT/Bearer.
- **Spiegazione per esterni:** È la faccia pubblica del ristorante. Prende gli ordini dai clienti (app mobile), controlla se sono chi dicono di essere (Login), passa il biglietto al Cuoco (Application) e porta il piatto pronto al cliente al tavolo.

---

## ⚙️ 3. I Lavoratori in Background (Il Retro Bottega)

### 🟠 `Xplore.Worker` (La Donna delle Pulizie e l'Operaio ai Lavori Pesanti)
- **Cosa fa:** Gira senza che l'utente se ne accorga. Non ha interfaccia. Ascolta i messaggi e fa i compiti estremamente lenti in background.
- **Cosa ci trovi:** I `Consumers` (es: Chi sente che è stato caricato un PDF). Lavora chiamando Qdrant per le funzioni RAG (estrazione testo, creazione embeddings vettoriali).
- **Spiegazione per esterni:** Se l'API (Il cameriere) dovesse elaborare un PDF da 300 pagine mentre l'utente aspetta, l'app si bloccherebbe. Allora l'API prende il PDF, lo butta in un cestino, e risponde subito all'utente "Ricevuto!". Il Worker è l'operario che prende quel PDF dal cestino e lavora per 5 minuti, senza disturbare nessuno.

### 🟣 `Xplore.Contracts` (I Bigliettini)
- **Cosa fa:** È una cartella piccolissima condivisa tra `API` e `Worker`.
- **Cosa ci trovi:** Messaggi ed Eventi. Ad esempio: la classe C# `DocumentUploadedEvent`.
- **Spiegazione per esterni:** L'App e il Worker non si parlano direttamente. Quando l'App carica un documento, scrive un bigliettino (Contract) e lo lascia sulla bacheca (RabbitMQ). Il Worker legge quel bigliettino. Entrambi devono sapere che forma ha quel bigliettino!

---

## 🚀 4. L'Avvio di Tutto (.NET Aspire - Il Direttore Generale)

Oggi le app non sono un file singolo. Sono un'API + Un Database + Redis + RabbitMQ + Un Worker + Un Vector Database. Come facciamo ad accendere 6 cose insieme senza ammattire sul terminale?

### 🔋 `Xplore.AppHost` (Il Regista dello Spettacolo)
- **Cosa fa:** È il "punto di partenza" (Startup Project) dell'intero ecosistema sul tuo PC.
- **Cosa ci trovi:** Un programma che, al posto di rispondere via web, si occupa di svegliare i container Docker, lanciare il Database PostgreSQL, lanciare RabbitMQ, Qdrant, avviare il progetto API e avviare il progetto Worker, collegandoli tutti tra loro magicamente senza configurare lunghe stringhe di connessione manuali.
- **Spiegazione per esterni:** Invece di dover aprire 5 terminali sul PC per lanciare tutte le tecnologie singolarmente, Aspire stringe le mani a tutte le parti e ti preme il pulsante "Accendi". Fornisce anche un'elegante Dashboard web!

### 🔧 `Xplore.ServiceDefaults` (Le Regole Aziendali)
- **Cosa fa:** Un progetto condiviso tra API e Worker per la Telemetria.
- **Cosa ci trovi:** Configurazione per OpenTelemetry, Health Checks (controlli della salute del server), e metriche.
- **Spiegazione per esterni:** Le regole generali dell'azienda: ogni lavoratore (API o Worker) deve mettersi una telecamera sul cappello che mi registra ogni suo errore o rallentamento (Telemetria) così posso vederlo in diretta nella Dashboard di AppHost.

---

## 💻 Come Far Girare Tutto (Come Accendere il Ristorante)

Per accendere letteralmente **tutta l'infrastruttura** con un solo click e avviare l'API, il Database SQL (Docker), il broker RabbitMQ e il Worker asyncrono:

#### Dal Terminale:
1. Apri la cartella base di Xplore
2. Esegui il comando:
   ```bash
   dotnet run --project src/Xplore.AppHost
   ```
3. Ti apparirà a schermo un log con scritto `Login to the dashboard at: http://localhost:15234` (la porta cambia).
4. Clicca su quel link!

#### Da Visual Studio:
1. Imposta **`Xplore.AppHost`** come "Progetto d'Avvio" (Startup Project).
2. Premi **F5** (Play).
3. Si aprirà il browser con l'elegante **Dashboard di .NET Aspire**.
4. Da lì, potrai vedere con i tuoi occhi il progetto `api` e il progetto `worker` che girano, leggere i loro terminali separati (Logs), e ottenere l'URL a cui espongono lo Swagger o le chiamate esterne!
