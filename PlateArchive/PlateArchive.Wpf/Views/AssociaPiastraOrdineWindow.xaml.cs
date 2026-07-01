using System.Windows;
using PlateArchive.Wpf.ViewModels;

namespace PlateArchive.Wpf.Views;

public partial class AssociaPiastraOrdineWindow : Window
{
    public AssociaPiastraOrdineWindow(AssociaPiastraOrdineViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RichiestaChiusura += (_, _) => Close();
    }
}
