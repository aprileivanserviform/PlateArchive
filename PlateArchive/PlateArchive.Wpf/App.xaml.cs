using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlateArchive.Data;
using PlateArchive.Data.Repositories.Implementations;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Services;
using PlateArchive.Wpf.Services;
using PlateArchive.Wpf.ViewModels;
using PlateArchive.Wpf.Views;
using System.Windows;

namespace PlateArchive.Wpf;

/// <summary>
/// Entry point dell'applicazione WPF.
/// Responsabilità:
/// <list type="number">
///   <item>Lettura della configurazione da appsettings.json.</item>
///   <item>Registrazione del container DI (Microsoft.Extensions.DependencyInjection).</item>
///   <item>Navigazione iniziale alla Dashboard.</item>
///   <item>Avvio della sincronizzazione clienti in background (senza bloccare l'UI).</item>
/// </list>
/// </summary>
public partial class App : Application
{
    /// <summary>ServiceProvider globale — accessibile dal code-behind per risolvere ViewModel on-demand.</summary>
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        RegistraGestioneErrori();

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();

        var services = new ServiceCollection();

        // ─── Database (SQLite) ─────────────────────────────────────────────────
        // La stringa di connessione deve essere presente in appsettings.json.
        // Il file .db vive sulla cartella di rete condivisa (stesso host dei disegni).
        var connStr = config.GetConnectionString("PlateArchiveDB")
            ?? throw new InvalidOperationException("Stringa di connessione 'PlateArchiveDB' non trovata in appsettings.json");
        services.AddDbContext<PlateArchiveDbContext>(opt =>
            opt.UseSqlite(connStr));

        // ─── Repository (Scoped) ──────────────────────────────────────────────
        // Scoped = una istanza per scope DI (una per schermata — NavigationService crea un nuovo scope ad ogni navigazione).
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IMacchinaStandardRepository, MacchinaStandardRepository>();
        services.AddScoped<IPiastraRepository, PiastraRepository>();
        services.AddScoped<IDisegnoRepository, DisegnoRepository>();
        services.AddScoped<ICompatibilitaRepository, CompatibilitaRepository>();
        services.AddScoped<IClienteMacchinaRepository, ClienteMacchinaRepository>();
        services.AddScoped<IClientePiastraRepository, ClientePiastraRepository>();
        services.AddScoped<ICategoriaPiastraRepository, CategoriaPiastraRepository>();
        services.AddScoped<IFormatoMacchinaRepository, FormatoMacchinaRepository>();
        services.AddScoped<IProduttoreMacchinaRepository, ProduttoreMacchinaRepository>();
        services.AddScoped<INotaTecnicaClienteRepository, NotaTecnicaClienteRepository>();
        services.AddScoped<IAllegatoClienteRepository, AllegatoClienteRepository>();

        // ─── Servizi infrastrutturali ─────────────────────────────────────────

        // NavigationService è Singleton perché deve sopravvivere a tutti i cambi schermata.
        services.AddSingleton<NavigationService>();

        // CartellaCondivisaDisegni: percorso UNC dove vengono archiviati i file disegno.
        var cartellaCondivisa = config["CartellaCondivisaDisegni"] ?? string.Empty;
        services.AddSingleton<IFileArchivioService>(new FileArchivioService(cartellaCondivisa));

        // Servizio di sincronizzazione con il gestionale DB2 via ODBC.
        // La query è configurabile in appsettings.json (Db2:QueryClienti).
        var db2ConnStr = config["Db2:ConnectionString"] ?? string.Empty;
        var db2Query   = config["Db2:QueryClienti"]     ?? "SELECT cv.ID_CLIENTE, bb.CLIRASOC FROM THIP.CLIENTI_VEN cv INNER JOIN FINANCE.BBCLIPT bb ON cv.ID_AZIENDA = bb.T01CD AND cv.ID_CLIENTE = bb.CLICD WHERE cv.STATO = 'V' AND cv.ID_AZIENDA = '001'";
        services.AddTransient<ISincronizzazioneGestionaleService>(sp =>
            new SincronizzazioneGestionaleService(
                db2ConnStr,
                db2Query,
                sp.GetRequiredService<IClienteRepository>()));

        // Lettura live (nessuna cache) delle righe ordine di vendita non evase.
        var db2QueryRighe = config["Db2:QueryRigheOrdineVendita"] ?? string.Empty;
        services.AddTransient<IRigheOrdineVenditaService>(_ =>
            new RigheOrdineVenditaService(db2ConnStr, db2QueryRighe));

        // SyncStatusService: aggiorna la barra di stato durante la sincronizzazione.
        services.AddSingleton<ISyncStatusService, SyncStatusService>();

        // ColumnLayoutService: persiste larghezza e ordine colonne DataGrid in %AppData%.
        services.AddSingleton<IColumnLayoutService, ColumnLayoutService>();

        // ─── ViewModels (Transient) ───────────────────────────────────────────
        // Transient = una nuova istanza ogni volta che NavigationService chiede un ViewModel.
        // Questo garantisce che ogni navigazione parta con stato pulito.
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ClientiViewModel>();
        services.AddTransient<ClienteDettaglioViewModel>();
        services.AddTransient<PiastreViewModel>();
        services.AddTransient<MacchineViewModel>();
        services.AddTransient<ImportaDisegnoViewModel>();
        services.AddTransient<PiastraDettaglioViewModel>();
        services.AddTransient<AssociaPiastraOrdineViewModel>();
        services.AddTransient<FormatiMacchinaViewModel>();
        services.AddTransient<CategoriePiastreViewModel>();
        services.AddTransient<ProduttoriMacchinaViewModel>();
        services.AddTransient<NuovaPiastraDialogViewModel>();
        services.AddTransient<OrdiniVenditaViewModel>();

        // MainWindow e MainWindowViewModel sono Singleton (vivono per tutta la sessione dell'app).
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        var provider = services.BuildServiceProvider();
        ServiceProvider = provider;

        // Applica automaticamente le migrazioni pendenti allo schema SQLite.
        // Sostituisce il vecchio workflow di script SQL manuali: con SQLite l'aggiornamento
        // schema è un'operazione locale e leggera, non serve più un intervento manuale separato.
        try
        {
            using var migrationScope = provider.CreateScope();
            migrationScope.ServiceProvider.GetRequiredService<PlateArchiveDbContext>().Database.Migrate();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Impossibile aggiornare/raggiungere il database:\n{ex.Message}\n\nVerificare la connessione alla cartella di rete condivisa.",
                "Errore database",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        // Naviga alla Dashboard come schermata iniziale.
        var navigation = provider.GetRequiredService<NavigationService>();
        navigation.Navigate<DashboardViewModel>();

        var mainWindow = provider.GetRequiredService<MainWindow>();
        mainWindow.Show();

        // Avvia la sincronizzazione clienti in background senza bloccare l'avvio dell'app.
        AvviaSyncInBackground(provider);
    }

    /// <summary>
    /// Rete di sicurezza contro gli errori non gestiti.
    /// <para>
    /// I comandi dei ViewModel usano <c>RelayCommand</c>, che riceve un <c>Action&lt;object?&gt;</c>:
    /// le lambda <c>async _ =&gt; await ...</c> diventano quindi <b>async void</b>, e un errore al loro
    /// interno (es. violazione di un vincolo di unicità al salvataggio) non è catturabile da chi
    /// invoca il comando — arriva direttamente qui. Senza questi handler l'applicazione si
    /// chiuderebbe di colpo, senza messaggio e perdendo il lavoro non salvato.
    /// </para>
    /// </summary>
    private void RegistraGestioneErrori()
    {
        // Errori sul thread UI: sono quelli delle lambda async void dei comandi.
        DispatcherUnhandledException += (_, e) =>
        {
            MostraErrore(e.Exception);
            e.Handled = true;   // l'app resta aperta
        };

        // Errori in Task non attesi (es. operazioni in background).
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            e.SetObserved();
            Dispatcher.Invoke(() => MostraErrore(e.Exception));
        };
    }

    private static void MostraErrore(Exception ex) =>
        MessageBox.Show(TraduciErrore(ex), "Operazione non riuscita", MessageBoxButton.OK, MessageBoxImage.Warning);

    /// <summary>
    /// Traduce l'errore tecnico in un messaggio comprensibile all'utente.
    /// Usata dalla rete di sicurezza globale, quando non si sa quale operazione fosse in corso.
    /// Metodo separato da <see cref="MostraErrore"/> per poter essere verificato senza aprire finestre.
    /// </summary>
    internal static string TraduciErrore(Exception ex) =>
        CausaErrore(ex) ?? $"Si è verificato un errore imprevisto:\n\n{Radice(ex).Message}";

    /// <summary>
    /// Descrive la causa dell'errore in una frase, senza riferimenti all'operazione in corso.
    /// Restituisce <c>null</c> se l'errore non rientra tra i casi noti.
    /// <para>
    /// I ViewModel la compongono con la propria descrizione dell'operazione per ottenere
    /// un messaggio contestuale (es. "Impossibile salvare la macchina 'X'. Esiste già...").
    /// </para>
    /// </summary>
    internal static string? CausaErrore(Exception ex) => Radice(ex).Message switch
    {
        var m when m.Contains("UNIQUE constraint failed") =>
            "Esiste già un elemento con questo codice: i codici devono essere univoci.",

        var m when m.Contains("FOREIGN KEY constraint failed") =>
            "Un elemento collegato non esiste più nell'archivio: potrebbe essere stato "
            + "eliminato da un'altra postazione. Aggiornare la schermata e riprovare.",

        var m when m.Contains("NOT NULL constraint failed") =>
            "Manca un dato obbligatorio: compilare tutti i campi richiesti.",

        var m when m.Contains("database is locked") || m.Contains("SQLITE_BUSY") =>
            "Il database è momentaneamente occupato da un'altra postazione: "
            + "attendere qualche secondo e riprovare.",

        var m when m.Contains("unable to open database file") =>
            "Impossibile raggiungere il database sulla cartella di rete condivisa: "
            + "verificare la connessione di rete.",

        _ => null,
    };

    /// <summary>L'errore reale di SQLite è annidato dentro la DbUpdateException di EF Core.</summary>
    private static Exception Radice(Exception ex)
    {
        var e = ex is AggregateException agg ? agg.Flatten().InnerException ?? agg : ex;
        while (e.InnerException is not null) e = e.InnerException;
        return e;
    }

    /// <summary>
    /// Esegue la sincronizzazione con il gestionale DB2 su un thread pool separato.
    /// Usa un nuovo DI scope per ottenere un repository/DbContext dedicato al task.
    /// Il risultato viene pubblicato tramite ISyncStatusService → barra di stato in MainWindow.
    /// </summary>
    private static void AvviaSyncInBackground(IServiceProvider provider)
    {
        var statusService = provider.GetRequiredService<ISyncStatusService>();
        statusService.SetRunning();

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = provider.CreateScope();
                var sync   = scope.ServiceProvider.GetRequiredService<ISincronizzazioneGestionaleService>();
                var result = await sync.SincronizzaClientiAsync();
                statusService.SetCompleted(result);
            }
            catch (Exception ex)
            {
                statusService.SetCompleted(new SincronizzazioneResult(0, 0, 0, ex.Message));
            }
        });
    }
}
