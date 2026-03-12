# 🚀 Masterclass: Sviluppo Multi-Progetto con Everything Claude Code (ECC)

**Progetti di riferimento:**
* **Backend:** `C:\Users\Fabbo\XploreRepos\Xplore` (.NET / C#)
* **Mobile:** `C:\Users\Fabbo\XploreRepos\XploreApp` (Kotlin Multiplatform / Compose)

**Filosofia di base (La Regola d'Oro):**
Mai mischiare i contesti. Apri sempre due terminali separati. Il Backend non deve sapere come funziona la UI del Mobile, e il Mobile deve solo leggere l'output (i modelli/DTO) del Backend usando percorsi relativi (`..\Xplore\...`). Questo azzera le allucinazioni e minimizza il consumo di token.

---

## 🧠 1. Il Ciclo di Vita della Memoria (Hooks)
Quando usi la CLI ufficiale (`claude`) nei tuoi terminali, ECC attiva degli script invisibili (Hooks) all'apertura e alla chiusura.

* **Apertura (`claude`):** Legge automaticamente `CLAUDE.md`, carica le regole e cerca se c'è un riassunto della sessione precedente.
* **Lavoro:** Consuma token *solo* per i file che gli chiedi di leggere.
* **Chiusura (`/exit` o `Ctrl+C`):** ECC intercetta la chiusura, comprime tutto quello che hai fatto in un piccolo riassunto di testo e lo salva localmente. La prossima volta che apri, saprà a che punto sei rimasto senza doversi rileggere centinaia di righe di codice.

---

## 🛠 Caso Studio 1: Implementazione Semplice (Solo Backend)
**Obiettivo:** Aggiungere un nuovo campo `Bio` all'entità `User` nel database e aggiornare l'API.

1. Apri il terminale in `Xplore` e avvia `claude`.
2. **Pianificazione e Codice:** Non dirgli semplicemente "fallo", usa il Planner per fargli seguire le regole del progetto.
   > `@.agents/planner.md Devo aggiungere una stringa 'Bio' all'entità User. Aggiorna il modello Entity Framework, genera il comando per la migration e aggiorna lo UserDTO. Segui le best practice C# salvate nelle nostre skills.`
3. Claude modificherà i file. Se ti serve eseguire un comando da terminale (es. `dotnet ef migrations add AddBio`), digli:
   > `Esegui il comando per creare la migration ed esegui l'update del database.`
4. **Salvataggio Memoria:** Hai finito il task. Scrivi `/exit`. In background, ECC scriverà il riassunto della sessione e lo salverà.

---

## 🚀 Caso Studio 2: Implementazione Complessa Cross-Project
**Obiettivo:** Creare un sistema di "Preferenze Notifiche". Il Backend deve esporre l'API, il Mobile deve creare la schermata UI e collegarla.

### Fase A: Il Backend (`Xplore`)
1. Apri il terminale in `Xplore` -> `claude`.
2. **Prompt:**
   > `@.agents/architect.md Crea un controller NotificationPreferencesController.cs con endpoint GET e PUT. Crea i relativi DTO. Segui la nostra architettura .NET.`
3. Controlla il codice generato.
4. Scrivi `/exit` per consolidare la memoria del backend.

### Fase B: Il Mobile (`XploreApp`)
1. Apri il terminale in `XploreApp` -> `claude`.
2. **Il Prompt Magico (Cross-Referencing):** Qui sta la potenza. L'AI mobile ha la mente pulita, quindi le passiamo solo il necessario dal backend.
   > `Stiamo implementando le Preferenze Notifiche nell'app KMP. Vai a leggere esattamente questi due file dal backend per capire la struttura dei dati: `
   > `1. ..\Xplore\Controllers\Models\NotificationPrefDTO.cs`
   > `2. ..\Xplore\Controllers\NotificationPreferencesController.cs`
   > `Ora, basandoti sulle nostre regole in skills/kotlin-standards/best-practices.md, scrivi la data class Kotlin usando kotlinx.serialization, il Ktor Client per chiamare l'API, il ViewModel e infine la UI in Compose Multiplatform.`
3. Claude leggerà i DTO in C#, capirà i nomi dei campi JSON, e scriverà codice Kotlin/Compose immacolato e strettamente tipizzato. Nessun token sprecato a leggere tutto il backend.
4. Scrivi `/exit` per salvare la sessione mobile.

---

## 🎓 Caso Studio 3: Continuous Learning (Quando Claude Sbaglia)
**Obiettivo:** Claude commette un errore ricorrente (es. gestisce male lo stato di Compose su iOS) e vogliamo insegnargli a non farlo mai più.

1. Sei nel terminale di `XploreApp` e noti l'errore. Lo correggi insieme a Claude.
2. Invece di ignorare la cosa, usa questo prompt:
   > `Questo è un pattern fondamentale che dobbiamo ricordare per KMP. Estrai la soluzione che abbiamo appena trovato sulla gestione dello stato in iOS. Aggiorna il file skills/kotlin-standards/best-practices.md aggiungendo una nuova sezione chiara e in bullet-point su come evitare questo bug in futuro.`
3. Claude aggiorna il file delle sue stesse "Skill".
4. Da domani, ogni volta che gli chiederai codice Compose, lui leggerà in automatico quel file e **non ripeterà più l'errore**.

---

## 🔍 Caso Studio 4: Audit e Sicurezza prima di un Commit
**Obiettivo:** Verificare che le nuove API C# non abbiano vulnerabilità.

1. Apri il terminale in `Xplore` -> `claude`.
2. Chiama lo specialista:
   > `@.agents/security-reviewer.md Ho appena finito di implementare il sistema di login. Fai un audit di sicurezza dei file AuthController.cs e UserService.cs. Controlla iniezioni SQL, validazione dei token JWT e gestione delle password. Sii spietato.`
3. L'agente applicherà regole rigide di sicurezza concentrandosi solo sulle falle.

---

## 📋 Best Practices Riepilogative
* **Usa `/exit` regolarmente:** Non tenere una singola chat di Claude aperta per giorni. Chiudila alla fine di ogni feature per far "respirare" la context window e innescare i riassunti in background.
* **Path Relativi sono tuoi amici:** Usa sempre `..\NomeAltroProgetto\percorso\file` per far comunicare i due mondi senza mischiarli.