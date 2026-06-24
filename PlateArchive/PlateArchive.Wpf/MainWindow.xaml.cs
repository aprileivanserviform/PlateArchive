using PlateArchive.Wpf.ViewModels;
using System.Windows;

namespace PlateArchive.Wpf;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
