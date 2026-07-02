using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PlateArchive.Wpf.ViewModels;

namespace PlateArchive.Wpf.Views;

public partial class AssociaPiastraOrdineWindow : Window
{
    public AssociaPiastraOrdineWindow(AssociaPiastraOrdineViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RichiestaChiusura += (_, _) => Close();
    }

    // ── Drop zone "crea nuova piastra" ──────────────────────────
    // Stesse estensioni accettate dal drag&drop della lista Piastre.

    private static readonly string[] _estensioniAccettate = [".dwg", ".dxf", ".pdf"];

    private static bool IsValidFileDrop(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return false;
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        return files?.Length == 1
            && _estensioniAccettate.Contains(Path.GetExtension(files[0]).ToLower());
    }

    private void DropZonaNuovaPiastra_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = IsValidFileDrop(e) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void DropZonaNuovaPiastra_Drop(object sender, DragEventArgs e)
    {
        if (!IsValidFileDrop(e)) return;
        if (DataContext is not AssociaPiastraOrdineViewModel vm) return;

        var file = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];

        // Riusa il flusso di importazione disegno: form con le specifiche piastra,
        // codice articolo gestionale e descrizione già compilati dalla riga ordine.
        var importaVm = App.ServiceProvider.GetRequiredService<ImportaDisegnoViewModel>();
        await importaVm.InitAsync(file, vm.CodiceArticolo, vm.DescrizioneArticolo);
        new ImportaDisegnoWindow(importaVm) { Owner = this }.ShowDialog();

        if (importaVm.Confermato)
            vm.SegnalaPiastraCreata();
    }
}
