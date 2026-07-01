using System.Windows.Controls;
using System.Windows.Input;
using PlateArchive.Core.Models;
using PlateArchive.Wpf.ViewModels;

namespace PlateArchive.Wpf.Views;

public partial class ClientiView : UserControl
{
    public ClientiView() => InitializeComponent();

    private void Row_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGridRow { DataContext: Cliente cliente } && DataContext is ClientiViewModel vm)
            vm.AprirDettaglioCommand.Execute(cliente);
    }
}
