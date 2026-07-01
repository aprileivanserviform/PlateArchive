using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using PlateArchive.Wpf.ViewModels;

namespace PlateArchive.Wpf.Views;

public partial class OrdiniVenditaView : UserControl
{
    public OrdiniVenditaView() => InitializeComponent();

    private void AssociaPiastra_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: RigaOrdineVenditaRow row }) return;
        _ = ApriAssociaPiastraAsync(row);
    }

    private void Riga_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGridRow { DataContext: RigaOrdineVenditaRow row }) return;

        if (row.PiastraTrovata)
            _ = ApriDettaglioPiastraAsync(row);
        else
            _ = ApriAssociaPiastraAsync(row);
    }

    private async Task ApriDettaglioPiastraAsync(RigaOrdineVenditaRow row)
    {
        if (row.Piastra is null) return;

        var vm = App.ServiceProvider.GetRequiredService<PiastraDettaglioViewModel>();
        await vm.InitAsync(row.Piastra.IdPiastra);
        new PiastraDettaglioWindow(vm) { Owner = Window.GetWindow(this) }.ShowDialog();
    }

    private async Task ApriAssociaPiastraAsync(RigaOrdineVenditaRow row)
    {
        if (DataContext is not OrdiniVenditaViewModel viewModel) return;

        var vm = App.ServiceProvider.GetRequiredService<AssociaPiastraOrdineViewModel>();
        await vm.InitAsync(row.Riga.CodiceArticolo);
        new AssociaPiastraOrdineWindow(vm) { Owner = Window.GetWindow(this) }.ShowDialog();

        if (vm.Confermato)
            await viewModel.RicaricaRigaAsync(row);
    }
}
