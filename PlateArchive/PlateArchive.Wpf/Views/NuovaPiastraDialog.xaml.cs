using System.Windows;
using PlateArchive.Wpf.ViewModels;

namespace PlateArchive.Wpf.Views;

public partial class NuovaPiastraDialog : Window
{
    public NuovaPiastraDialog()
    {
        InitializeComponent();
    }

    private static bool IsValidFileDrop(DragEventArgs e) =>
        e.Data.GetDataPresent(DataFormats.FileDrop)
        && e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 };

    private void DisegnoDropZone_DragOver(object sender, DragEventArgs e)
    {
        if (IsValidFileDrop(e))
        {
            e.Effects = DragDropEffects.Copy;
            DisegnoDropOverlay.Visibility = Visibility.Visible;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void DisegnoDropZone_DragLeave(object sender, DragEventArgs e)
    {
        if (sender is UIElement el)
        {
            var pos  = e.GetPosition(el);
            var size = el.RenderSize;
            if (pos.X < 0 || pos.Y < 0 || pos.X > size.Width || pos.Y > size.Height)
                DisegnoDropOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void DisegnoDropZone_Drop(object sender, DragEventArgs e)
    {
        DisegnoDropOverlay.Visibility = Visibility.Collapsed;
        if (!IsValidFileDrop(e)) return;
        if (DataContext is not NuovaPiastraDialogViewModel vm) return;
        vm.PercorsoDisegnoPendente = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
    }
}
