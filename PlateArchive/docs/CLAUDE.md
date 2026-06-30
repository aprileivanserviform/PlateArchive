# PlateArchive — CLAUDE.md

Applicazione desktop WPF (.NET) per la gestione dell'archivio tecnico-commerciale **Cliente – Macchina – Piastra – Disegno**. Collegata a un database relazionale (SQL Server / SQLite).

## Struttura soluzione

```
PlateArchive.sln
├── PlateArchive.Wpf       # Viste XAML, ViewModel, risorse, navigazione
├── PlateArchive.Core      # Modelli di dominio, enum, validazione
├── PlateArchive.Data      # DbContext (EF Core), repository, migrazioni
└── PlateArchive.Services  # Sync DB2, apertura file, percorsi, compatibilità
```

Pattern architetturale: **MVVM** (Views / ViewModels / Models / Services / Commands / Repositories).

## Build e comandi essenziali

- `dotnet build` — build soluzione
- `dotnet ef migrations add <Nome>` — nuova migrazione EF Core (da `PlateArchive.Data`)
- `dotnet ef database update` — applica migrazioni al database
- `dotnet test` — esegui test (se presenti)

## Principio fondamentale

**La Piastra è l'elemento centrale del sistema.** Il codice piastra corrisponde (o è direttamente collegato) all'articolo gestionale usato per vendita e produzione.

```
Codice articolo gestionale → Piastra → Disegno tecnico
```

## Regole di dominio (rispettarle sempre)

1. Una piastra può avere **più disegni** tecnici (relazione N:M via `DisegniPiastre`); lo stesso disegno può essere associato a più piastre (es. varianti di formato).
2. Una piastra può essere compatibile con **più macchine** standard (N:N via `PiastraMacchinaCompatibile`).
3. L'associazione **cliente–macchina è opzionale**: una piastra può essere associata a un cliente senza specificare la macchina (`ClientePiastra.IdClienteMacchina` è nullable).
4. La compatibilità piastra–macchina è un dato tecnico standard, indipendente dal cliente.
5. I file disegno **non** si salvano nel database: solo metadati e percorso file (server condiviso o Autodesk Vault).
6. I clienti provengono dal gestionale DB2; DB2 è la fonte primaria dell'anagrafica cliente.
7. Il codice macchina deve essere standardizzato (`CodiceMacchina` univoco) per evitare duplicati.

## Entità principali (PlateArchive.Core)

| Classe | Chiave | Note |
|--------|--------|------|
| `Cliente` | `IdCliente` | Sync da DB2, `CodiceClienteGestionale` univoco |
| `MacchinaStandard` | `IdMacchinaStandard` | `CodiceMacchina` univoco |
| `Piastra` | `IdPiastra` | `CodicePiastra` univoco, `CodiceArticoloGestionale` univoco |
| `Disegno` | `IdDisegno` | N:M con Piastra via `DisegniPiastre` |
| `DisegnoPiastra` | `IdDisegnoPiastra` | Chiave composta `(IdDisegno, IdPiastra)` univoca |
| `PiastraMacchinaCompatibile` | `IdCompatibilita` | Chiave composta `(IdPiastra, IdMacchinaStandard)` univoca |
| `ClienteMacchina` | `IdClienteMacchina` | Natura commerciale/anagrafica |
| `ClientePiastra` | `IdClientePiastra` | `IdClienteMacchina` nullable; vincolo `(IdCliente, IdPiastra)` univoco (v1) |

## Navigazione prevista

- **Da cliente** → macchine associate → piastre compatibili
- **Da cliente** → piastre associate → disegno tecnico
- **Da macchina** → piastre compatibili → disegni tecnici
- **Da piastra** → disegno tecnico / macchine compatibili / clienti associati
- **Da ordine (futura evoluzione)** → codice articolo → piastra → disegno

## Schermate WPF

- `Dashboard` — ricerca rapida, ultime piastre, disegni da verificare
- `ClientiView` / `ClienteDettaglioView` — anagrafica + macchine + piastre + compatibilità
- `PiastreView` — CRUD piastre, disegno associato, macchine compatibili
- `MacchineView` — CRUD macchine standard, piastre compatibili, clienti associati
- `DisegniView` — percorso file, revisione, stato, apertura file

## Convenzioni di codice

- Usare `PascalCase` per classi, proprietà e metodi; `camelCase` per variabili locali.
- Ogni `ViewModel` implementa `INotifyPropertyChanged`.
- I comandi WPF usano `RelayCommand` / `ICommand`.
- `PlateArchive.Data` espone interfacce repository; le implementazioni concrete usano EF Core.
- Non usare code-behind nelle View: tutta la logica va nel ViewModel.

## Archivio disegni

I file (DWG / DXF / PDF) risiedono su server condiviso aziendale o Autodesk Vault.
Nel DB si salva: `CodiceDisegno`, `NomeFile`, `PercorsoFile`, `VaultId` (opzionale), `Revisione`, `Formato`, `Stato`, `DataUltimaModificaFile`.

## Criticità note

- L'archivio storico può contenere nomi macchina duplicati o non standardizzati: applicare sempre la ricerca per `CodiceMacchina` normalizzato.
- La prima versione gestisce una sola revisione corrente per disegno (storico revisioni è evolutivo).
- Integrazione Autodesk Vault prevista in una fase successiva; per ora salvare solo `PercorsoFile`.
