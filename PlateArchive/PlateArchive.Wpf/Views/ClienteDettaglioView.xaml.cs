using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PlateArchive.Core.Models;
using PlateArchive.Wpf.ViewModels;

namespace PlateArchive.Wpf.Views;

public partial class ClienteDettaglioView : UserControl
{
    public ClienteDettaglioView() => InitializeComponent();

    private async void ApriDettaglioPiastra_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: ClientePiastra cp }) return;

        var vm = App.ServiceProvider.GetRequiredService<PiastraDettaglioViewModel>();
        await vm.InitAsync(cp.IdPiastra);
        new PiastraDettaglioWindow(vm) { Owner = Window.GetWindow(this) }.ShowDialog();
    }
}
