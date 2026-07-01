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

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();

        var services = new ServiceCollection();

        // ─── Database ─────────────────────────────────────────────────────────
        // La stringa di connessione deve essere presente in appsettings.json.
        // Per lo sviluppo locale si può usare SQLite cambiando il provider.
        var connStr = config.GetConnectionString("PlateArchiveDB")
            ?? throw new InvalidOperationException("Stringa di connessione 'PlateArchiveDB' non trovata in appsettings.json");
        services.AddDbContext<PlateArchiveDbContext>(opt =>
            opt.UseSqlServer(connStr));

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
        services.AddTransient<OrdiniVenditaViewModel>();

        // MainWindow e MainWindowViewModel sono Singleton (vivono per tutta la sessione dell'app).
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        var provider = services.BuildServiceProvider();
        ServiceProvider = provider;

        // Naviga alla Dashboard come schermata iniziale.
        var navigation = provider.GetRequiredService<NavigationService>();
        navigation.Navigate<DashboardViewModel>();

        var mainWindow = provider.GetRequiredService<MainWindow>();
        mainWindow.Show();

        // Avvia la sincronizzazione clienti in background senza bloccare l'avvio dell'app.
        AvviaSyncInBackground(provider);
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
