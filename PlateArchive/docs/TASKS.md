# Task di sviluppo — PlateArchive

> Documento aggiornato man mano che i task vengono implementati.  
> Stato: `[ ]` = da fare · `[~]` = in corso · `[x]` = completato · `[!]` = bloccato/decisione aperta

---

## TASK-01 — Struttura soluzione multi-progetto

**Priorità:** Alta  
**Stato:** `[x]` — completato 2026-06-23

### Contesto

La soluzione attuale (`PlateArchive.slnx`) contiene un solo progetto WPF con template vuoto (`App.xaml`, `MainWindow.xaml`). L'analisi funzionale (§17) richiede una suddivisione in quattro progetti separati per mantenere la separazione delle responsabilità: `Core` per il dominio, `Data` per la persistenza, `Services` per la logica applicativa, `Wpf` per la UI.

### Obiettivo

Ristrutturare la soluzione aggiungendo i tre progetti mancanti e configurando i riferimenti tra di essi, in modo che ogni progetto dipenda solo dai livelli sotto di lui.

### File e interventi

| Progetto | Tipo | Riferimenti |
|---|---|---|
| `PlateArchive.Core` (nuovo) | Class Library .NET 8 | — |
| `PlateArchive.Data` (nuovo) | Class Library .NET 8 | `PlateArchive.Core` |
| `PlateArchive.Services` (nuovo) | Class Library .NET 8 | `PlateArchive.Core`, `PlateArchive.Data` |
| `PlateArchive` → rinominato in `PlateArchive.Wpf` | WPF Application .NET 8 | `PlateArchive.Core`, `PlateArchive.Data`, `PlateArchive.Services` |

**`PlateArchive.slnx`** — aggiungere i tre nuovi `<Project>`:

```xml
<Solution>
  <Project Path="PlateArchive.Wpf\PlateArchive.Wpf.csproj" />
  <Project Path="PlateArchive.Core\PlateArchive.Core.csproj" />
  <Project Path="PlateArchive.Data\PlateArchive.Data.csproj" />
  <Project Path="PlateArchive.Services\PlateArchive.Services.csproj" />
</Solution>
```

**`PlateArchive.Wpf.csproj`** — aggiungere i `<ProjectReference>`:

```xml
<ItemGroup>
  <ProjectReference Include="..\PlateArchive.Core\PlateArchive.Core.csproj" />
  <ProjectReference Include="..\PlateArchive.Data\PlateArchive.Data.csproj" />
  <ProjectReference Include="..\PlateArchive.Services\PlateArchive.Services.csproj" />
</ItemGroup>
```

### Acceptance criteria

- [x] `dotnet build` sulla soluzione compila tutti e quattro i progetti senza errori
- [x] I namespace rispecchiano il nome progetto (`PlateArchive.Core`, `PlateArchive.Data`, ecc.)
- [x] Il progetto `Wpf` mantiene `App.xaml` e `MainWindow.xaml` funzionanti

---

## TASK-02 — Modelli di dominio (PlateArchive.Core)

**Priorità:** Alta  
**Stato:** `[x]` — completato 2026-06-23

### Contesto

Le sette entità del sistema (§12, §19) devono vivere in `PlateArchive.Core` per essere accessibili da tutti i layer senza creare dipendenze circolari. Le regole di dominio fondamentali (piastra come elemento centrale, relazione 1:1 piastra-disegno, macchina cliente opzionale) si riflettono direttamente nella struttura delle classi.

### Obiettivo

Creare tutte le classi di dominio con le proprietà definite nell'analisi, più gli enum necessari per i campi di stato.

### File da creare in `PlateArchive.Core`

#### Enums (`Enums/`)

```
Enums/
├── StatoPiastra.cs        (Attiva, Obsoleta, DaVerificare)
├── StatoDisegno.cs        (Attivo, Obsoleto, DaVerificare)
├── StatoCliente.cs        (Attivo, Disattivato, Storico)
├── StatoClientePiastra.cs (Attiva, Obsoleta, Proposta, DaVerificare)
└── FonteDatoCompatibilita.cs (Disegno, Manuale, VerificaTecnica)
```

#### Modelli (`Models/`)

**`Cliente.cs`**
```csharp
public class Cliente
{
    public int IdCliente { get; set; }
    public string CodiceClienteGestionale { get; set; } = string.Empty;
    public string RagioneSociale { get; set; } = string.Empty;
    public string? PartitaIVA { get; set; }
    public string? CodiceFiscale { get; set; }
    public StatoCliente StatoCliente { get; set; }
    public DateTime? DataUltimaSincronizzazione { get; set; }
    public string? Note { get; set; }
    public ICollection<ClienteMacchina> Macchine { get; set; } = [];
    public ICollection<ClientePiastra> Piastre { get; set; } = [];
}
```

**`MacchinaStandard.cs`**
```csharp
public class MacchinaStandard
{
    public int IdMacchinaStandard { get; set; }
    public string CodiceMacchina { get; set; } = string.Empty;
    public string NomeMacchina { get; set; } = string.Empty;
    public string? Famiglia { get; set; }
    public string? Formato { get; set; }
    public string? Versione { get; set; }
    public string? Produttore { get; set; }
    public bool Attiva { get; set; } = true;
    public string? Note { get; set; }
    public ICollection<PiastraMacchinaCompatibile> PiastreCompatibili { get; set; } = [];
    public ICollection<ClienteMacchina> ClientiAssociati { get; set; } = [];
}
```

**`Piastra.cs`**
```csharp
public class Piastra
{
    public int IdPiastra { get; set; }
    public string CodicePiastra { get; set; } = string.Empty;
    public string? CodiceArticoloGestionale { get; set; }
    public string? Descrizione { get; set; }
    public StatoPiastra Stato { get; set; }
    public string? Note { get; set; }
    public DateTime DataCreazione { get; set; }
    public DateTime DataUltimaModifica { get; set; }
    public Disegno? Disegno { get; set; }
    public ICollection<PiastraMacchinaCompatibile> MacchineCompatibili { get; set; } = [];
    public ICollection<ClientePiastra> ClientiAssociati { get; set; } = [];
}
```

**`Disegno.cs`**
```csharp
public class Disegno
{
    public int IdDisegno { get; set; }
    public int IdPiastra { get; set; }
    public string? CodiceDisegno { get; set; }
    public string? NomeFile { get; set; }
    public string? PercorsoFile { get; set; }
    public string? VaultId { get; set; }        // predisposto per Autodesk Vault
    public string? Revisione { get; set; }
    public string? Formato { get; set; }        // DWG, DXF, PDF
    public StatoDisegno Stato { get; set; }
    public DateTime? DataUltimaModificaFile { get; set; }
    public string? Note { get; set; }
    public Piastra Piastra { get; set; } = null!;
}
```

**`PiastraMacchinaCompatibile.cs`**
```csharp
public class PiastraMacchinaCompatibile
{
    public int IdCompatibilita { get; set; }
    public int IdPiastra { get; set; }
    public int IdMacchinaStandard { get; set; }
    public FonteDatoCompatibilita? FonteDato { get; set; }
    public DateTime? DataVerifica { get; set; }
    public string? UtenteVerifica { get; set; }
    public bool Attiva { get; set; } = true;
    public string? Note { get; set; }
    public Piastra Piastra { get; set; } = null!;
    public MacchinaStandard MacchinaStandard { get; set; } = null!;
}
```

**`ClienteMacchina.cs`**
```csharp
public class ClienteMacchina
{
    public int IdClienteMacchina { get; set; }
    public int IdCliente { get; set; }
    public int IdMacchinaStandard { get; set; }
    public string? Matricola { get; set; }
    public string? CodiceInternoCliente { get; set; }
    public DateTime DataAssociazione { get; set; }
    public bool Attiva { get; set; } = true;
    public string? Note { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public MacchinaStandard MacchinaStandard { get; set; } = null!;
}
```

**`ClientePiastra.cs`**
```csharp
public class ClientePiastra
{
    public int IdClientePiastra { get; set; }
    public int IdCliente { get; set; }
    public int IdPiastra { get; set; }
    public int? IdClienteMacchina { get; set; }   // nullable: macchina opzionale
    public DateTime DataAssociazione { get; set; }
    public StatoClientePiastra Stato { get; set; }
    public string? Note { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public Piastra Piastra { get; set; } = null!;
    public ClienteMacchina? ClienteMacchina { get; set; }
}
```

### Acceptance criteria

- [x] Tutti i modelli compilano senza warning nullable
- [x] Gli enum coprono tutti i valori definiti nell'analisi funzionale
- [x] Nessun riferimento a EF Core o a librerie UI in `PlateArchive.Core`

---

## TASK-03 — Database e EF Core (PlateArchive.Data)

**Priorità:** Alta  
**Stato:** `[x]` — completato 2026-06-23

### Contesto

La persistenza usa Entity Framework Core. Il database di riferimento è **SQLite** per sviluppo e prototipazione (zero configurazione, file locale); la struttura è predisposta per migrare a SQL Server in produzione cambiando solo il provider e la stringa di connessione.

Le regole di unicità e la relazione 1:1 piastra-disegno devono essere enforce a livello di database, non solo applicativo.

### Obiettivo

Creare il `DbContext`, configurare tutti i vincoli tramite Fluent API, implementare le interfacce repository e applicare la migrazione iniziale.

### Pacchetti NuGet da installare in `PlateArchive.Data`

```
Microsoft.EntityFrameworkCore
Microsoft.EntityFrameworkCore.Sqlite
Microsoft.EntityFrameworkCore.Tools
```

### File da creare in `PlateArchive.Data`

**`PlateArchiveDbContext.cs`** — vincoli principali tramite Fluent API:

```csharp
protected override void OnModelCreating(ModelBuilder mb)
{
    mb.Entity<Cliente>()
        .HasIndex(c => c.CodiceClienteGestionale).IsUnique();

    mb.Entity<MacchinaStandard>()
        .HasIndex(m => m.CodiceMacchina).IsUnique();

    mb.Entity<Piastra>()
        .HasIndex(p => p.CodicePiastra).IsUnique();
    mb.Entity<Piastra>()
        .HasIndex(p => p.CodiceArticoloGestionale).IsUnique();

    // 1:1 Piastra → Disegno (IdPiastra UNIQUE in Disegni)
    mb.Entity<Disegno>()
        .HasIndex(d => d.IdPiastra).IsUnique();
    mb.Entity<Disegno>()
        .HasOne(d => d.Piastra)
        .WithOne(p => p.Disegno)
        .HasForeignKey<Disegno>(d => d.IdPiastra);

    // N:N Piastra ↔ MacchinaStandard
    mb.Entity<PiastraMacchinaCompatibile>()
        .HasIndex(x => new { x.IdPiastra, x.IdMacchinaStandard }).IsUnique();

    // v1: un cliente può avere una piastra una sola volta
    mb.Entity<ClientePiastra>()
        .HasIndex(x => new { x.IdCliente, x.IdPiastra }).IsUnique();
}
```

**Struttura cartelle:**

```
PlateArchive.Data/
├── PlateArchiveDbContext.cs
├── Configurations/          (opzionale: IEntityTypeConfiguration<T> per entità complesse)
├── Migrations/              (generato da EF Core)
└── Repositories/
    ├── Interfaces/
    │   ├── IClienteRepository.cs
    │   ├── IMacchinaStandardRepository.cs
    │   ├── IPiastraRepository.cs
    │   ├── IDisegnoRepository.cs
    │   ├── ICompatibilitaRepository.cs
    │   ├── IClienteMacchinaRepository.cs
    │   └── IClientePiastraRepository.cs
    └── Implementations/
        ├── ClienteRepository.cs
        ├── MacchinaStandardRepository.cs
        ├── PiastraRepository.cs
        ├── DisegnoRepository.cs
        ├── CompatibilitaRepository.cs
        ├── ClienteMacchinaRepository.cs
        └── ClientePiastraRepository.cs
```

**Comandi EF Core** (eseguire da `PlateArchive.Data`):

```bash
dotnet ef migrations add InitialCreate --startup-project ../PlateArchive.Wpf
dotnet ef database update --startup-project ../PlateArchive.Wpf
```

### Acceptance criteria

- [x] `dotnet ef migrations add InitialCreate` genera la migrazione senza errori
- [x] `dotnet ef database update` crea il file `.db` con tutte le tabelle
- [x] Il vincolo `UNIQUE (IdPiastra)` su `Disegni` impedisce fisicamente due disegni per la stessa piastra
- [x] Il vincolo `UNIQUE (IdCliente, IdPiastra)` su `ClientePiastra` impedisce duplicati
- [x] I repository espongono almeno: `GetByIdAsync`, `GetAllAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`

---

## TASK-04 — Infrastruttura MVVM e navigazione (PlateArchive.Wpf)

**Priorità:** Alta  
**Stato:** `[x]` — completato 2026-06-24

### Contesto

L'applicazione WPF usa il pattern MVVM. Servono una classe base per i ViewModel, un'implementazione di `ICommand`, un sistema di navigazione tra schermate e la configurazione della Dependency Injection. Queste fondamenta vengono usate da tutti i TASK successivi.

### Obiettivo

Creare l'infrastruttura condivisa del layer `Wpf` e configurare il container DI in `App.xaml.cs`.

### File da creare in `PlateArchive.Wpf`

**Pacchetti NuGet da installare:**
```
Microsoft.Extensions.DependencyInjection
Microsoft.Extensions.Configuration.Json
```

**`Commands/RelayCommand.cs`**
```csharp
public class RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? p) => canExecute?.Invoke(p) ?? true;
    public void Execute(object? p) => execute(p);
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
```

**`ViewModels/ViewModelBase.cs`**
```csharp
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}
```

**`Services/NavigationService.cs`** — gestione navigazione tramite `ContentControl`:
- Esporre `Navigate<TViewModel>()` che aggiorna la `CurrentViewModel` visibile in `MainWindow`
- Registrare tutti i ViewModel nel container DI

**`App.xaml.cs`** — configurazione DI:
```csharp
protected override void OnStartup(StartupEventArgs e)
{
    var services = new ServiceCollection();

    // Database
    services.AddDbContext<PlateArchiveDbContext>(opt =>
        opt.UseSqlite("Data Source=platearchive.db"));

    // Repository
    services.AddScoped<IClienteRepository, ClienteRepository>();
    services.AddScoped<IMacchinaStandardRepository, MacchinaStandardRepository>();
    services.AddScoped<IPiastraRepository, PiastraRepository>();
    services.AddScoped<IDisegnoRepository, DisegnoRepository>();
    services.AddScoped<ICompatibilitaRepository, CompatibilitaRepository>();
    services.AddScoped<IClienteMacchinaRepository, ClienteMacchinaRepository>();
    services.AddScoped<IClientePiastraRepository, ClientePiastraRepository>();

    // Services
    services.AddSingleton<NavigationService>();

    // ViewModels
    services.AddTransient<DashboardViewModel>();
    services.AddTransient<ClientiViewModel>();
    // ... altri ViewModel

    // Window
    services.AddSingleton<MainWindow>();

    var provider = services.BuildServiceProvider();
    var mainWindow = provider.GetRequiredService<MainWindow>();
    mainWindow.Show();
}
```

### Acceptance criteria

- [x] `RelayCommand` risponde correttamente a `CanExecuteChanged`
- [x] `SetField` in `ViewModelBase` notifica il binding solo se il valore cambia effettivamente
- [x] La navigazione tra due ViewModel di test funziona aggiornando il `ContentControl` in `MainWindow`
- [x] Tutti i servizi e i repository si risolvono correttamente dal container DI all'avvio

---

## TASK-05 — Dashboard principale

**Priorità:** Media  
**Stato:** `[x]` — completato 2026-06-24

### Contesto

La Dashboard è la prima schermata che l'utente vede all'avvio. Deve consentire l'accesso rapido alle funzioni principali e dare una visione immediata delle attività recenti (ultime piastre inserite, disegni da verificare).

### Obiettivo

Creare `DashboardView.xaml` e il relativo `DashboardViewModel` con le funzioni di ricerca rapida e i widget informativi.

### File da creare

```
PlateArchive.Wpf/
└── Views/
    └── DashboardView.xaml
ViewModels/
    └── DashboardViewModel.cs
```

### Layout schermata

```
┌──────────────────────────────────────────────────────────────────┐
│  PlateArchive                                              [≡]   │
├────────────────────────────────────────────────────────────────  │
│  Ricerca rapida                                                   │
│  [🔍 Cliente...       ] [🔍 Piastra...        ] [🔍 Macchina...] │
├───────────────────────────────┬──────────────────────────────────│
│  Ultime piastre inserite      │  Disegni da verificare           │
│  PLT-000245  NOVACUT 106  ... │  PLT-000312  Revisione B   ...   │
│  PLT-000312  ...              │  PLT-000418  ...                 │
│  PLT-000418  ...              │                                  │
├───────────────────────────────┴──────────────────────────────────│
│  [+ Nuova Piastra]  [+ Nuova Macchina]  [↻ Sincronizza Clienti] │
└──────────────────────────────────────────────────────────────────┘
```

### Dettagli implementativi

- Le ricerche rapide navigano rispettivamente a `ClientiView`, `PiastreView`, `MacchineView` pre-impostando il filtro di testo
- "Ultime piastre" = `IPiastraRepository.GetAllAsync()` ordinato per `DataCreazione DESC`, limite 10
- "Disegni da verificare" = disegni con `Stato = DaVerificare`, limite 10
- I pulsanti rapidi aprono il form di creazione corrispondente o invocano la sincronizzazione DB2

### Acceptance criteria

- [x] Le tre barre di ricerca rapida navigano alle schermate corrette passando il testo di ricerca
- [x] La lista "Ultime piastre" si aggiorna ogni volta che si torna alla Dashboard
- [x] I pulsanti rapidi sono visibili e funzionanti (possono aprire schermate vuote negli sprint iniziali)

---

## TASK-06 — Schermata Clienti

**Priorità:** Alta  
**Stato:** `[x]` — completato 2026-06-24

### Contesto

I clienti vengono sincronizzati da DB2 e sono in sola lettura per i dati anagrafici. La schermata serve a navigare il parco clienti, vedere le macchine e le piastre associate, e gestire le associazioni.

### Obiettivo

Creare la lista clienti con ricerca e il dettaglio cliente con sezioni macchine, piastre e compatibilità.

### File da creare

```
Views/
├── ClientiView.xaml                  (lista + ricerca)
└── ClienteDettaglioView.xaml         (dettaglio)
ViewModels/
├── ClientiViewModel.cs
└── ClienteDettaglioViewModel.cs
```

### Layout — Lista clienti (`ClientiView`)

```
┌─────────────────────────────────────────────────────┐
│  Clienti                                            │
│  [🔍 Cerca per codice, ragione sociale, P.IVA... ] │
├─────────────────────────────────────────────────────│
│  Codice      Ragione Sociale         Stato          │
│  ─────────────────────────────────────────────────  │
│  CLI-001     Rossi S.r.l.            Attivo  [→]   │
│  CLI-002     Bianchi & C.            Attivo  [→]   │
│  CLI-003     Verdi Macchine          Storico [→]   │
└─────────────────────────────────────────────────────┘
```

### Layout — Dettaglio cliente (`ClienteDettaglioView`)

```
┌────────────────────────────────────────────────────────────┐
│  ← Rossi S.r.l.   (CLI-001)                    [Sync ↻]  │
├──────────────────────────────────────────────────────────  │
│  [Dati anagrafici]  [Macchine]  [Piastre]  [Compatibilità]│
├──────────────────────────────────────────────────────────  │
│  TAB: Macchine                         [+ Aggiungi]        │
│  ────────────────────────────────────────────────────────  │
│  NOVACUT 106    Matricola: 12345    Attiva   [✕]          │
│  EXPERTCUT 106  —                   Attiva   [✕]          │
├──────────────────────────────────────────────────────────  │
│  TAB: Piastre                          [+ Aggiungi]        │
│  ────────────────────────────────────────────────────────  │
│  PLT-000245  NOVACUT 106  Attiva  [📄 Apri disegno] [✕]  │
│  PLT-000312  —            Attiva  [📄 Apri disegno] [✕]  │
└────────────────────────────────────────────────────────────┘
```

### Flusso "Aggiungi macchina" (dalla tab Macchine)

1. Si apre un dialog/popup di ricerca nell'archivio `MacchineStandard`
2. L'utente cerca e seleziona la macchina
3. Inserisce matricola (opzionale) e note (opzionale)
4. Salva → crea record in `ClienteMacchina`
5. La tab Macchine si aggiorna; compare la nuova macchina con le piastre compatibili nella tab Compatibilità

### Flusso "Aggiungi piastra" (dalla tab Piastre) → vedi TASK-10 per il dettaglio

### Acceptance criteria

- [x] La ricerca filtra in tempo reale su codice, ragione sociale e P.IVA
- [x] Il tab "Macchine" mostra tutte le `ClienteMacchina` del cliente con macchina e matricola
- [x] Il tab "Piastre" mostra tutte le `ClientePiastra` del cliente con lo stato e il pulsante "Apri disegno"
- [x] Il tab "Compatibilità" mostra le piastre compatibili con le macchine del cliente (da `PiastraMacchinaCompatibile`)
- [x] Il pulsante "Apri disegno" è attivo solo se la piastra ha un disegno con `PercorsoFile` valorizzato
- [x] I dati anagrafici sono in sola lettura (fonte DB2)

---

## TASK-07 — Schermata Macchine Standard

**Priorità:** Alta  
**Stato:** `[x]` — completato 2026-06-24

### Contesto

L'archivio macchine standard è il punto di riferimento per le compatibilità. Il rischio principale (§23.2) è la duplicazione di codici macchina con varianti tipografiche. Serve un controllo anti-duplicati attivo durante l'inserimento.

### Obiettivo

Creare la lista macchine con ricerca, il form di creazione/modifica con controllo duplicati, e le sezioni di navigazione verso piastre compatibili e clienti associati.

### File da creare

```
Views/
└── MacchineView.xaml
ViewModels/
└── MacchineViewModel.cs
```

### Layout

```
┌──────────────────────────────────────────────────────────────┐
│  Macchine Standard                           [+ Nuova]       │
│  [🔍 Cerca per codice, nome, famiglia...  ]                  │
│  [☐ Solo attive]                                             │
├──────────────────────────────────────────────────────────────│
│  Codice            Nome             Famiglia  Formato  Att.  │
│  ─────────────────────────────────────────────────────────   │
│  NOVACUT_106       NOVACUT 106      NOVACUT   106      ✔    │
│  EXPERTCUT_106     EXPERTCUT 106    EXPERTCUT 106      ✔    │
│  SPRINTERA_106PER  SPRINTERA 106 P… SPRINTERA 106      ✔    │
│                                                    [→ Det.]  │
├──────────────────────────────────────────────────────────────│
│  Dettaglio: NOVACUT 106                                      │
│  Piastre compatibili: PLT-000245, PLT-000312, PLT-000418     │
│  Clienti che la possiedono: Rossi S.r.l., Verdi Macchine     │
└──────────────────────────────────────────────────────────────┘
```

### Form inserimento / modifica

Campi obbligatori: `CodiceMacchina`, `NomeMacchina`  
Campi opzionali: `Famiglia`, `Formato`, `Versione`, `Produttore`, `Note`

**Controllo anti-duplicati** — durante la digitazione di `CodiceMacchina`:
- Confronto case-insensitive e normalizzato (rimuovere spazi, underscore, trattini)
- Se esiste già un codice simile, mostrare avviso inline: _"Attenzione: esiste già NOVACUT_106. Procedere?"_
- Il salvataggio non è bloccato (l'utente può ignorare l'avviso), ma il DB enforcement (`UNIQUE CodiceMacchina`) impedisce duplicati esatti

### Acceptance criteria

- [x] La ricerca filtra su codice, nome e famiglia in tempo reale
- [x] Il checkbox "Solo attive" nasconde le macchine con `Attiva = false`
- [x] Durante l'inserimento, l'avviso di duplicato potenziale appare entro 300ms dalla digitazione (debounce)
- [x] La lista piastre compatibili mostra `CodicePiastra` e `Descrizione`
- [x] La lista clienti associati mostra `CodiceClienteGestionale` e `RagioneSociale`
- [x] La disabilitazione di una macchina (`Attiva = false`) non cancella le associazioni esistenti

---

## TASK-08 — Schermata Piastre

**Priorità:** Alta  
**Stato:** `[x]` — completato 2026-06-24

### Contesto

La piastra è l'elemento centrale del sistema (§3). La schermata deve permettere la navigazione completa: da una piastra si deve poter raggiungere il disegno, le macchine compatibili e i clienti associati.

### Obiettivo

Creare la lista piastre con ricerca e il form di creazione/modifica con collegamento a disegno e macchine compatibili.

### File da creare

```
Views/
└── PiastreView.xaml
ViewModels/
└── PiastreViewModel.cs
```

### Layout

```
┌──────────────────────────────────────────────────────────────┐
│  Piastre                                      [+ Nuova]      │
│  [🔍 Codice piastra...] [🔍 Cod. articolo...] [Stato ▼]     │
├──────────────────────────────────────────────────────────────│
│  Codice       Art. Gestionale  Descrizione           Stato   │
│  ────────────────────────────────────────────────────────    │
│  PLT-000245   PLT-000245       Piastra frontale 106  Attiva  │
│  PLT-000312   PLT-000312       Piastra laterale 106  Attiva  │
│                                               [→ Dettaglio]  │
├──────────────────────────────────────────────────────────────│
│  Dettaglio: PLT-000245                                       │
│  ──── Disegno ───────────────────────────────────────────    │
│  PLT-000245.dwg  Rev. B  Attivo  [📄 Apri]  [✏ Modifica]   │
│  ──── Macchine compatibili ──────────────────────────────    │
│  NOVACUT 106 · EXPERTCUT 106 · SPRINTERA 106 PER   [+ Add]  │
│  ──── Clienti associati ─────────────────────────────────    │
│  Rossi S.r.l. (NOVACUT 106) · Verdi Macchine (—)            │
└──────────────────────────────────────────────────────────────┘
```

### Form inserimento nuova piastra

Campi obbligatori: `CodicePiastra`  
Campi opzionali: `CodiceArticoloGestionale`, `Descrizione`, `Stato`, `Note`  

Flusso guidato:
1. Inserimento dati base piastra → salvataggio
2. (Opzionale) Collegamento disegno → apre mini-form disegno (vedi TASK-11)
3. (Opzionale) Aggiunta macchine compatibili → dialog di selezione multipla da `MacchineStandard`

### Acceptance criteria

- [x] La ricerca filtra su `CodicePiastra`, `CodiceArticoloGestionale` e `Descrizione`
- [x] Il filtro per stato funziona (Attiva / Obsoleta / Da verificare / Tutti)
- [x] Il form impedisce il salvataggio se `CodicePiastra` è già presente (errore inline)
- [x] Dalla sezione "Macchine compatibili" si possono aggiungere e rimuovere associazioni
- [x] Dalla sezione "Disegno" si può aprire il file se `PercorsoFile` è valorizzato

---

## TASK-09 — Schermata Disegni

**Priorità:** Media  
**Stato:** `[x]` — completato 2026-06-24

### Contesto

I disegni non hanno una vita indipendente dalla piastra (relazione 1:1), ma può essere utile avere una vista dedicata per cercare disegni da verificare, aggiornare percorsi file o gestire revisioni.

### Obiettivo

Creare una schermata di consultazione e modifica metadati disegno, accessibile anche dalla scheda piastra.

### File da creare

```
Views/
└── DisegniView.xaml
ViewModels/
└── DisegniViewModel.cs
```

### Layout

```
┌──────────────────────────────────────────────────────────────┐
│  Disegni                                                     │
│  [🔍 Cerca per codice disegno o piastra...] [Stato ▼]       │
├──────────────────────────────────────────────────────────────│
│  Codice        Piastra       Rev.  Formato  Stato            │
│  ────────────────────────────────────────────────────────    │
│  PLT-000245    PLT-000245    B     DWG      Attivo           │
│  PLT-000312    PLT-000312    A     DWG      Da verificare    │
├──────────────────────────────────────────────────────────────│
│  Modifica: PLT-000245                                        │
│  Percorso file: [\\server\disegni\PLT-000245.dwg    ] [📂]  │
│  Revisione:     [B]          Formato: [DWG ▼]               │
│  Stato:         [Attivo ▼]   Data ult. mod.: 2026-03-10      │
│  Note:          [                                         ]  │
│  [💾 Salva]                   [📄 Apri file]                │
└──────────────────────────────────────────────────────────────┘
```

### Acceptance criteria

- [x] La ricerca filtra per `CodiceDisegno` e `CodicePiastra` associata
- [x] Il filtro stato mostra i disegni "Da verificare" in cima o evidenziati
- [x] Il pulsante `[📂]` apre un `OpenFileDialog` e popola il campo percorso
- [x] Il pulsante "Apri file" usa `Process.Start` per aprire il file con l'applicazione predefinita
- [x] Se il file non è raggiungibile al percorso indicato, mostrare messaggio: _"File non trovato al percorso indicato"_ (non eccezione)

---

## TASK-10 — Compatibilità piastra-macchina

**Priorità:** Alta  
**Stato:** `[ ]`

### Contesto

La compatibilità tecnica (§6, §7.1) è il legame tra piastra e macchina standard. Queste informazioni oggi sono leggibili visivamente sui disegni e devono diventare dati strutturati nel database (§22). È il dato che permette al software di mostrare automaticamente le piastre proposte a un cliente in base alla sua macchina.

### Obiettivo

Implementare la gestione delle compatibilità (aggiunta, rimozione, consultazione) da entrambe le direzioni: dalla scheda piastra e dalla scheda macchina.

### Interventi

**`ICompatibilitaRepository`** — metodi necessari:
```csharp
Task<IEnumerable<PiastraMacchinaCompatibile>> GetByPiastraAsync(int idPiastra);
Task<IEnumerable<PiastraMacchinaCompatibile>> GetByMacchinaAsync(int idMacchinaStandard);
Task<bool> ExistsAsync(int idPiastra, int idMacchinaStandard);
Task AddAsync(PiastraMacchinaCompatibile compatibilita);
Task SetAttivaAsync(int idCompatibilita, bool attiva);
```

**Dalla scheda piastra** (TASK-08):
- Sezione "Macchine compatibili" con dialog di selezione multipla da `MacchineStandard`
- Campo `FonteDato` nel dialog (Disegno / Manuale / Verifica Tecnica)
- Rimozione = `SetAttivaAsync(id, false)` (soft delete, non cancellazione)

**Dalla scheda macchina** (TASK-07):
- Sezione "Piastre compatibili" con navigazione alla scheda piastra

**Dalla scheda cliente** (TASK-06):
- Tab "Compatibilità": query `PiastraMacchinaCompatibile` filtrata sulle macchine del cliente

### Acceptance criteria

- [ ] L'aggiunta di una compatibilità già esistente non crea duplicati (gestita a livello DB e applicativo)
- [ ] La disabilitazione (`Attiva = false`) non compare nelle liste di compatibilità attive
- [ ] Dalla scheda cliente, la tab "Compatibilità" mostra solo le piastre compatibili con almeno una macchina del cliente
- [ ] Il campo `FonteDato` è obbligatorio all'inserimento

---

## TASK-11 — Associazioni cliente (macchina e piastra)

**Priorità:** Alta  
**Stato:** `[ ]`

### Contesto

L'associazione commerciale tra cliente e piastra (§7.2, §13.3) è il flusso operativo centrale del software. La regola chiave: la macchina è opzionale — si può associare una piastra a un cliente anche senza indicare una macchina specifica.

### Obiettivo

Implementare il flusso guidato di associazione piastra-cliente con proposta intelligente della macchina compatibile.

### Flusso "Aggiungi piastra a cliente" (dialog guidato)

```
Step 1 — Selezione piastra
┌────────────────────────────────────────────────────────────────┐
│  Aggiungi piastra a Rossi S.r.l.                    [×]        │
│  [🔍 Cerca piastra per codice o descrizione...   ]            │
│  PLT-000245  Piastra frontale 106                              │
│  PLT-000312  Piastra laterale 106                              │
│  PLT-000418  Piastra posteriore 106                            │
│                                        [Avanti →]              │
└────────────────────────────────────────────────────────────────┘

Step 2 — Macchina (opzionale)
┌────────────────────────────────────────────────────────────────┐
│  Piastra: PLT-000245  →  Macchine compatibili:                 │
│  NOVACUT 106 · EXPERTCUT 106 · VISIONCUT 106                   │
│                                                                 │
│  Il cliente ha già una macchina compatibile:                   │
│  ● NOVACUT 106 — Matricola 12345                               │
│                                                                 │
│  [Collega a NOVACUT 106]  [Scegli altra macchina]  [Salta →]  │
└────────────────────────────────────────────────────────────────┘
  ↓ (se il cliente non ha macchine compatibili)
┌────────────────────────────────────────────────────────────────┐
│  Il cliente non ha macchine compatibili registrate.            │
│  [Associa macchina esistente]  [Salta →]                       │
└────────────────────────────────────────────────────────────────┘

Step 3 — Conferma
┌────────────────────────────────────────────────────────────────┐
│  Riepilogo associazione:                                        │
│  Cliente: Rossi S.r.l.                                         │
│  Piastra: PLT-000245 — Piastra frontale 106                    │
│  Macchina: NOVACUT 106 (Matricola 12345)  [opzionale]          │
│  Stato: [Attiva ▼]     Note: [              ]                  │
│  [← Indietro]                              [💾 Salva]          │
└────────────────────────────────────────────────────────────────┘
```

### Interventi

**`IClientePiastraRepository`** — metodi necessari:
```csharp
Task<IEnumerable<ClientePiastra>> GetByClienteAsync(int idCliente);
Task<bool> ExistsAsync(int idCliente, int idPiastra);
Task AddAsync(ClientePiastra clientePiastra);
Task UpdateAsync(ClientePiastra clientePiastra);
Task DeleteAsync(int idClientePiastra);
```

### Acceptance criteria

- [ ] Step 1 esclude le piastre già associate al cliente
- [ ] Step 2 propone automaticamente le macchine compatibili con la piastra selezionata
- [ ] Step 2 evidenzia le macchine del cliente che sono anche compatibili con la piastra
- [ ] L'utente può procedere da Step 2 senza selezionare una macchina (pulsante "Salta")
- [ ] Il salvataggio senza macchina imposta `IdClienteMacchina = null` (non errore)
- [ ] Tentare di associare una piastra già associata mostra errore: _"Piastra già associata a questo cliente"_

---

## TASK-12 — Servizio apertura file disegno (PlateArchive.Services)

**Priorità:** Media  
**Stato:** `[ ]`

### Contesto

I file disegno risiedono su server condiviso aziendale o Autodesk Vault. Il software salva solo il percorso (§5); l'apertura avviene con l'applicazione predefinita di sistema (AutoCAD, DWG TrueView, ecc.). Serve un servizio che gestisca l'apertura e verifichi l'accessibilità del file.

### Obiettivo

Creare `IFileDisegnoService` in `PlateArchive.Services` e integrarlo nei ViewModel che mostrano il pulsante "Apri disegno".

### File da creare

**`PlateArchive.Services/FileDisegnoService.cs`**

```csharp
public interface IFileDisegnoService
{
    bool FileEsiste(string percorso);
    void ApriFile(string percorso);
    string? SelezionaFile();   // apre OpenFileDialog, restituisce percorso o null
}

public class FileDisegnoService : IFileDisegnoService
{
    public bool FileEsiste(string percorso) => File.Exists(percorso);

    public void ApriFile(string percorso)
    {
        if (!FileEsiste(percorso))
            throw new FileNotFoundException("File non trovato", percorso);
        Process.Start(new ProcessStartInfo(percorso) { UseShellExecute = true });
    }

    public string? SelezionaFile()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "File disegno|*.dwg;*.dxf;*.pdf|Tutti i file|*.*",
            Title = "Seleziona file disegno"
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }
}
```

### Acceptance criteria

- [ ] `ApriFile` apre correttamente un file `.dwg` con l'applicazione predefinita
- [ ] `ApriFile` con percorso non raggiungibile lancia `FileNotFoundException` — i ViewModel la intercettano e mostrano messaggio all'utente
- [ ] `SelezionaFile` restituisce `null` se l'utente chiude il dialog senza selezionare
- [ ] Il servizio è registrato nel container DI in `App.xaml.cs`

---

## TASK-13 — Sincronizzazione clienti da DB2

**Priorità:** Bassa (MVP: importazione manuale)  
**Stato:** `[ ]`

### Contesto

I clienti provengono dal gestionale aziendale DB2 (§9). DB2 rimane la fonte primaria: il software WPF mantiene una copia sincronizzata dei dati necessari all'archivio. La connessione avviene tramite **ODBC su VPN** già configurata dal fornitore del gestionale SaaS — non serve alcun setup di rete aggiuntivo. In prima versione la sincronizzazione è manuale on-demand (pulsante in Dashboard).

### Obiettivo

Implementare `IDb2SyncService` che legge i clienti dal gestionale DB2 via ODBC e fa upsert nella tabella `Clienti` locale, aggiornando `DataUltimaSincronizzazione`.

### Pacchetti NuGet da installare in `PlateArchive.Services`

```
System.Data.Odbc   // incluso nel runtime .NET 8, nessun pacchetto extra
```

**`PlateArchive.Services/Db2SyncService.cs`** — connessione ODBC:

```csharp
public interface IDb2SyncService
{
    Task<SyncResult> SincronizzaClientiAsync();
}

public class Db2SyncService(string odbcConnectionString, IClienteRepository repo) : IDb2SyncService
{
    public async Task<SyncResult> SincronizzaClientiAsync()
    {
        using var conn = new OdbcConnection(odbcConnectionString);
        await conn.OpenAsync();

        // Query da adattare ai nomi colonna effettivi del gestionale
        const string sql = """
            SELECT CODCLI, RAGSOC, PIVA, STATO
            FROM CLIENTI
            WHERE STATO <> 'C'
            """;

        using var cmd = new OdbcCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync();

        int inseriti = 0, aggiornati = 0;
        while (await reader.ReadAsync())
        {
            var codice = reader.GetString(0).Trim();
            var cliente = new Cliente
            {
                CodiceClienteGestionale = codice,
                RagioneSociale = reader.GetString(1).Trim(),
                PartitaIVA = reader.IsDBNull(2) ? null : reader.GetString(2).Trim(),
                StatoCliente = StatoCliente.Attivo,
                DataUltimaSincronizzazione = DateTime.UtcNow,
            };

            var esistente = await repo.GetByCodiceGestionaleAsync(codice);
            if (esistente is null) { await repo.AddAsync(cliente); inseriti++; }
            else
            {
                esistente.RagioneSociale = cliente.RagioneSociale;
                esistente.PartitaIVA = cliente.PartitaIVA;
                esistente.DataUltimaSincronizzazione = DateTime.UtcNow;
                await repo.UpdateAsync(esistente); aggiornati++;
            }
        }
        return new SyncResult(inseriti, aggiornati);
    }
}

public record SyncResult(int Inseriti, int Aggiornati);
```

> **Nota:** la stringa di connessione ODBC va inserita in `appsettings.json` (non nel codice).  
> Formato tipico: `DSN=GestionaleDB2;UID=xxx;PWD=yyy;` oppure connection string completa con driver IBM.  
> La VPN deve essere attiva affinché la connessione riesca.

### Flusso upsert

```csharp
// Per ogni cliente letto da DB2:
var esistente = await repo.GetByCodiceGestionaleAsync(cliente.CodiceClienteGestionale);
if (esistente is null)
    await repo.AddAsync(cliente);
else
{
    esistente.RagioneSociale = cliente.RagioneSociale;
    esistente.StatoCliente = cliente.StatoCliente;
    esistente.DataUltimaSincronizzazione = DateTime.UtcNow;
    await repo.UpdateAsync(esistente);
}
```

### Acceptance criteria

- [ ] La sincronizzazione aggiorna `DataUltimaSincronizzazione` per ogni cliente elaborato
- [ ] I clienti eliminati in DB2 non vengono cancellati ma impostati a `StatoCliente = Storico`
- [ ] In caso di errore di connessione, il messaggio mostra causa e timestamp — nessuna perdita di dati locali
- [ ] La Dashboard mostra data/ora dell'ultima sincronizzazione riuscita

---

## Preparazione dati — Attività parallele allo sviluppo

> Queste attività non richiedono software completato: possono iniziare subito su file Excel/cartelle e essere importate nella prima versione del DB.

- [ ] Censire tutte le piastre esistenti nell'archivio cartaceo/file
- [ ] Assegnare `CodicePiastra` univoco a ogni piastra — formato provvisorio: `PLT-000001` (6 cifre con zero padding); da sostituire con la codifica aziendale interna in una fase successiva
- [ ] Collegare ogni piastra al percorso del file disegno corrispondente
- [ ] Leggere dai disegni le macchine compatibili indicate e trascriverle in un foglio
- [ ] Creare l'elenco normalizzato delle macchine standard con `CodiceMacchina` univoco
- [ ] Compilare le compatibilità piastra-macchina nel foglio di staging
- [ ] Importare clienti da DB2 (anche manualmente in prima battuta)
- [ ] Associare piastre ai clienti dove l'informazione è già disponibile

---

## Evolutivi futuri (fuori scope MVP)

| Funzione | Riferimento |
|---|---|
| Storico revisioni disegno (v1: una sola revisione corrente) | §23.3 |
| Integrazione Autodesk Vault tramite `VaultId` | §23.4 |
| Sincronizzazione DB2 automatica schedulata | §9 |
| Navigazione da ordine/articolo gestionale → piastra → disegno | §13.7 |
| Gestione utenti e permessi | — |
| Esportazione dati (CSV / Excel) | — |
