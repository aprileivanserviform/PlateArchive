using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PlateArchive.Wpf.ViewModels;

namespace PlateArchive.Wpf.Views;

public partial class DisegniView : UserControl
{
    public DisegniView() => InitializeComponent();

    private static readonly string[] _extensioniAccettate = [".dwg", ".dxf", ".pdf", ".stp", ".step", ".igs"];

    private static bool IsValidFileDrop(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return false;
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        return files?.Length == 1
            && _extensioniAccettate.Contains(Path.GetExtension(files[0]).ToLower());
    }

    private async void ApriImportaDialog(string percorsoFile)
    {
        var vm = App.ServiceProvider.GetRequiredService<ImportaDisegnoViewModel>();
        await vm.InitAsync(percorsoFile);
        var win = new ImportaDisegnoWindow(vm) { Owner = Window.GetWindow(this) };
        win.ShowDialog();

        // Aggiunge alla lista solo se era un disegno nuovo (non già presente nel DB prima dell'import).
        if (vm.Confermato && vm.DisegnoEsistente is null && vm.DisegnoCreato is not null
            && DataContext is DisegniViewModel disegniVm)
        {
            disegniVm.AggiungiDisegno(vm.DisegnoCreato);
        }
    }

    // ── Drop zone placeholder (nessun disegno selezionato) ──────────────────

    private void DropZonePlaceholder_DragOver(object sender, DragEventArgs e)
    {
        if (IsValidFileDrop(e))
        {
            e.Effects = DragDropEffects.Copy;
            DropZonePlaceholderOverlay.Visibility = Visibility.Visible;
        }
        else
        {
            e.Effects = DragDropEffects.None;
            DropZonePlaceholderOverlay.Visibility = Visibility.Collapsed;
        }
        e.Handled = true;
    }

    private void DropZonePlaceholder_DragLeave(object sender, DragEventArgs e)
    {
        if (sender is UIElement el)
        {
            var pos  = e.GetPosition(el);
            var size = el.RenderSize;
            if (pos.X < 0 || pos.Y < 0 || pos.X > size.Width || pos.Y > size.Height)
                DropZonePlaceholderOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private async void DropZonePlaceholder_Drop(object sender, DragEventArgs e)
    {
        DropZonePlaceholderOverlay.Visibility = Visibility.Collapsed;
        if (!IsValidFileDrop(e)) return;
        var file = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
        ApriImportaDialog(file);
        await Task.CompletedTask;
    }

    // ── Drop zone in fondo al pannello modifica (disegno selezionato) ────────

    private void DropZoneEdit_DragOver(object sender, DragEventArgs e)
    {
        if (IsValidFileDrop(e))
        {
            e.Effects = DragDropEffects.Copy;
            DropZoneEditOverlay.Visibility = Visibility.Visible;
        }
        else
        {
            e.Effects = DragDropEffects.None;
            DropZoneEditOverlay.Visibility = Visibility.Collapsed;
        }
        e.Handled = true;
    }

    private void DropZoneEdit_DragLeave(object sender, DragEventArgs e)
    {
        if (sender is UIElement el)
        {
            var pos  = e.GetPosition(el);
            var size = el.RenderSize;
            if (pos.X < 0 || pos.Y < 0 || pos.X > size.Width || pos.Y > size.Height)
                DropZoneEditOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void DropZoneEdit_Drop(object sender, DragEventArgs e)
    {
        DropZoneEditOverlay.Visibility = Visibility.Collapsed;
        if (!IsValidFileDrop(e)) return;
        var file = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
        ApriImportaDialog(file);
    }
}
