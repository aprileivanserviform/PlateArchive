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

        // Navigation e servizi
        services.AddSingleton<NavigationService>();

        var cartellaCondivisa = config["CartellaCondivisaDisegni"] ?? string.Empty;
        services.AddSingleton<IFileArchivioService>(new FileArchivioService(cartellaCondivisa));

        // ViewModels (Transient: nuova istanza a ogni navigazione via scope)
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

        // Navigazione iniziale alla Dashboard
        var navigation = provider.GetRequiredService<NavigationService>();
        navigation.Navigate<DashboardViewModel>();

        var mainWindow = provider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}
