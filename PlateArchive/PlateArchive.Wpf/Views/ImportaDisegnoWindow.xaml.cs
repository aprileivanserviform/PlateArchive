using PlateArchive.Wpf.ViewModels;
using System.Windows;

namespace PlateArchive.Wpf.Views;

public partial class ImportaDisegnoWindow : Window
{
    public ImportaDisegnoWindow(ImportaDisegnoViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RichiestaChiusura += (_, _) => Close();
    }
}
