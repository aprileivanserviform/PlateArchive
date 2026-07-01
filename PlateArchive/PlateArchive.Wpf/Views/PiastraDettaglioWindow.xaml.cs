using System.Windows;
using PlateArchive.Wpf.ViewModels;

namespace PlateArchive.Wpf.Views;

public partial class PiastraDettaglioWindow : Window
{
    public PiastraDettaglioWindow(PiastraDettaglioViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Chiudi_Click(object sender, RoutedEventArgs e) => Close();
}
