using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PlateArchive.Core.Models;
using PlateArchive.Wpf.ViewModels;

namespace PlateArchive.Wpf.Views;

public partial class PiastreView : UserControl
{
    public PiastreView() => InitializeComponent();

    private void Row_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGridRow { DataContext: Piastra } && DataContext is PiastreViewModel vm)
            vm.ModificaCommand.Execute(null);
    }

    private static readonly string[] _extensioniAccettate = [".dwg", ".dxf", ".pdf"];

    private static bool IsValidFileDrop(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return false;
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        return files?.Length == 1
            && _extensioniAccettate.Contains(
                Path.GetExtension(files[0]).ToLower());
    }

    private static Piastra? GetPiastraAtPoint(DataGrid grid, DragEventArgs e)
    {
        var hit = VisualTreeHelper.HitTest(grid, e.GetPosition(grid))?.VisualHit as DependencyObject;
        while (hit is not null)
        {
            if (hit is DataGridRow row) return row.Item as Piastra;
            hit = VisualTreeHelper.GetParent(hit);
        }
        return null;
    }

    // ── DataGrid ────────────────────────────────────────────────

    private void DataGrid_DragOver(object sender, DragEventArgs e)
    {
        if (IsValidFileDrop(e))
        {
            e.Effects = DragDropEffects.Copy;
            DataGridDropOverlay.Visibility = Visibility.Visible;
        }
        else
        {
            e.Effects = DragDropEffects.None;
            DataGridDropOverlay.Visibility = Visibility.Collapsed;
        }
        e.Handled = true;
    }

    private void DataGrid_DragLeave(object sender, DragEventArgs e)
    {
        if (sender is UIElement el)
        {
            var pos  = e.GetPosition(el);
            var size = el.RenderSize;
            if (pos.X < 0 || pos.Y < 0 || pos.X > size.Width || pos.Y > size.Height)
                DataGridDropOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private async void DataGrid_Drop(object sender, DragEventArgs e)
    {
        DataGridDropOverlay.Visibility = Visibility.Collapsed;
        if (!IsValidFileDrop(e)) return;
        if (DataContext is not PiastreViewModel vm) return;

        var file    = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
        var piastra = GetPiastraAtPoint(PiastreDataGrid, e) ?? vm.PiastraSelezionata;
        if (piastra is null) return;

        vm.PiastraSelezionata = piastra;
        await vm.AssociaDisegnoAsync(piastra, file);
    }

    // ── Sezione Disegno ─────────────────────────────────────────

    private void DisegnoSection_DragOver(object sender, DragEventArgs e)
    {
        var valido = IsValidFileDrop(e)
            && DataContext is PiastreViewModel { PiastraSelezionata: not null };

        e.Effects = valido ? DragDropEffects.Copy : DragDropEffects.None;
        DisegnoSectionOverlay.Visibility = valido ? Visibility.Visible : Visibility.Collapsed;
        e.Handled = true;
    }

    private void DisegnoSection_DragLeave(object sender, DragEventArgs e)
    {
        if (sender is UIElement el)
        {
            var pos  = e.GetPosition(el);
            var size = el.RenderSize;
            if (pos.X < 0 || pos.Y < 0 || pos.X > size.Width || pos.Y > size.Height)
                DisegnoSectionOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private async void DisegnoSection_Drop(object sender, DragEventArgs e)
    {
        DisegnoSectionOverlay.Visibility = Visibility.Collapsed;
        if (!IsValidFileDrop(e)) return;
        if (DataContext is not PiastreViewModel vm) return;
        if (vm.PiastraSelezionata is null) return;

        var file = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
        await vm.AssociaDisegnoAsync(vm.PiastraSelezionata, file);
    }

    // ── Drop zone nel form (file pendente) ──────────────────────

    private void FormDisegno_DragOver(object sender, DragEventArgs e)
    {
        if (IsValidFileDrop(e))
        {
            e.Effects = DragDropEffects.Copy;
            FormDisegnoOverlay.Visibility = Visibility.Visible;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void FormDisegno_DragLeave(object sender, DragEventArgs e)
    {
        if (sender is UIElement el)
        {
            var pos  = e.GetPosition(el);
            var size = el.RenderSize;
            if (pos.X < 0 || pos.Y < 0 || pos.X > size.Width || pos.Y > size.Height)
                FormDisegnoOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void FormDisegno_Drop(object sender, DragEventArgs e)
    {
        FormDisegnoOverlay.Visibility = Visibility.Collapsed;
        if (!IsValidFileDrop(e)) return;
        if (DataContext is not PiastreViewModel vm) return;
        vm.PercorsoDisegnoPendente = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
    }
}
