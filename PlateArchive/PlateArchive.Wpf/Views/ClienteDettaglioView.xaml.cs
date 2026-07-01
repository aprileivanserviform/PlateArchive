using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using PlateArchive.Core.Models;
using PlateArchive.Wpf.ViewModels;

namespace PlateArchive.Wpf.Views;

public partial class ClienteDettaglioView : UserControl
{
    public ClienteDettaglioView() => InitializeComponent();

    private void ApriDettaglioPiastra_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: ClientePiastra cp }) return;
        _ = ApriDettaglioPiastraAsync(cp);
    }

    private void RigaPiastra_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGridRow { DataContext: ClientePiastra cp }) return;
        _ = ApriDettaglioPiastraAsync(cp);
    }

    private async Task ApriDettaglioPiastraAsync(ClientePiastra cp)
    {
        var vm = App.ServiceProvider.GetRequiredService<PiastraDettaglioViewModel>();
        await vm.InitAsync(cp.IdPiastra);
        new PiastraDettaglioWindow(vm) { Owner = Window.GetWindow(this) }.ShowDialog();
    }
}
