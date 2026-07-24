using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PlateArchive.Wpf.ViewModels;

namespace PlateArchive.Wpf.Views;

public partial class SceltaPiastraOrdineWindow : Window
{
    public SceltaPiastraOrdineWindow(SceltaPiastraOrdineViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Chiudi_Click(object sender, RoutedEventArgs e) => Close();

    private void ApriDisegno_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PiastraCompatibile compatibile }) return;

        var percorso = compatibile.Piastra.Disegno?.PercorsoFile;
        if (string.IsNullOrEmpty(percorso)) return;

        if (!File.Exists(percorso))
        {
            MessageBox.Show(this, $"File non trovato: {percorso}", "Apri disegno",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(percorso) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Impossibile aprire il file: {ex.Message}", "Apri disegno",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApriDettaglio_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: PiastraCompatibile compatibile }) return;
        if (DataContext is not SceltaPiastraOrdineViewModel vm) return;
        _ = ApriDettaglioAsync(compatibile, vm.DescrizioneArticolo);
    }

    private async Task ApriDettaglioAsync(PiastraCompatibile compatibile, string descrizioneArticolo)
    {
        var vm = App.ServiceProvider.GetRequiredService<PiastraDettaglioViewModel>();
        await vm.InitAsync(compatibile.Piastra.IdPiastra, descrizioneArticolo);
        new PiastraDettaglioWindow(vm) { Owner = this }.ShowDialog();
    }
}
