using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlateArchive.Data;
using PlateArchive.Data.Repositories.Implementations;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Services;
using PlateArchive.Wpf.Services;
using PlateArchive.Wpf.ViewModels;
using System.Windows;

namespace PlateArchive.Wpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();

        var services = new ServiceCollection();

        // Database
        var connStr = config.GetConnectionString("PlateArchiveDB")
            ?? throw new InvalidOperationException("Stringa di connessione 'PlateArchiveDB' non trovata in appsettings.json");
        services.AddDbContext<PlateArchiveDbContext>(opt =>
            opt.UseSqlServer(connStr));

        // Repository
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IMacchinaStandardRepository, MacchinaStandardRepository>();
        services.AddScoped<IPiastraRepository, PiastraRepository>();
        services.AddScoped<IDisegnoRepository, DisegnoRepository>();
        services.AddScoped<ICompatibilitaRepository, CompatibilitaRepository>();
        services.AddScoped<IClienteMacchinaRepository, ClienteMacchinaRepository>();
        services.AddScoped<IClientePiastraRepository, ClientePiastraRepository>();
        services.AddScoped<ICategoriaPiastraRepository, CategoriaPiastraRepository>();
        services.AddScoped<IFamigliaMacchinaRepository, FamigliaMacchinaRepository>();
        services.AddScoped<IProduttoreMacchinaRepository, ProduttoreMacchinaRepository>();

        // Navigation e servizi
        services.AddSingleton<NavigationService>();

        var cartellaCondivisa = config["CartellaCondivisaDisegni"] ?? string.Empty;
        services.AddSingleton<IFileArchivioService>(new FileArchivioService(cartellaCondivisa));

        var db2ConnStr = config["Db2:ConnectionString"] ?? string.Empty;
        var db2Query   = config["Db2:QueryClienti"]     ?? "SELECT cv.ID_CLIENTE, bb.CLIRASOC FROM THIP.CLIENTI_VEN cv INNER JOIN FINANCE.BBCLIPT bb ON cv.ID_AZIENDA = bb.T01CD AND cv.ID_CLIENTE = bb.CLICD WHERE cv.STATO = 'V' AND cv.ID_AZIENDA = '001'";
        services.AddTransient<ISincronizzazioneGestionaleService>(sp =>
            new SincronizzazioneGestionaleService(
                db2ConnStr,
                db2Query,
                sp.GetRequiredService<IClienteRepository>()));

        // Stato sincronizzazione (singleton: visibile in tutta l'app)
        services.AddSingleton<ISyncStatusService, SyncStatusService>();

        // ViewModels
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ClientiViewModel>();
        services.AddTransient<ClienteDettaglioViewModel>();
        services.AddTransient<PiastreViewModel>();
        services.AddTransient<MacchineViewModel>();
        services.AddTransient<DisegniViewModel>();

        // MainWindow e suo ViewModel (Singleton: vivono per tutta la sessione)
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        var provider = services.BuildServiceProvider();

        var navigation = provider.GetRequiredService<NavigationService>();
        navigation.Navigate<DashboardViewModel>();

        var mainWindow = provider.GetRequiredService<MainWindow>();
        mainWindow.Show();

        // Sincronizzazione clienti in background — non blocca l'avvio
        AvviaSyncInBackground(provider);
    }

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
