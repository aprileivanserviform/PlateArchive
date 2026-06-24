using System.Windows.Controls;
using System.Windows.Input;
using PlateArchive.Core.Models;
using PlateArchive.Wpf.ViewModels;

namespace PlateArchive.Wpf.Views;

public partial class MacchineView : UserControl
{
    public MacchineView() => InitializeComponent();

    private void Row_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGridRow { DataContext: MacchinaStandard } && DataContext is MacchineViewModel vm)
            vm.ModificaCommand.Execute(null);
    }
}
