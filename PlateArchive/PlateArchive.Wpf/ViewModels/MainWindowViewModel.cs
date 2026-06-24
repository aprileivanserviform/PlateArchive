using System.Windows.Input;
using PlateArchive.Wpf.Commands;
using PlateArchive.Wpf.Services;

namespace PlateArchive.Wpf.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly NavigationService _navigation;

    public MainWindowViewModel(NavigationService navigation)
    {
        _navigation = navigation;
        navigation.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(NavigationService.CurrentViewModel))
                OnPropertyChanged(nameof(CurrentViewModel));
        };

        NavigateToDashboardCommand = new RelayCommand(_ => _navigation.Navigate<DashboardViewModel>());
        NavigateToClientiCommand   = new RelayCommand(_ => _navigation.Navigate<ClientiViewModel>());
        NavigateToPiastreCommand   = new RelayCommand(_ => _navigation.Navigate<PiastreViewModel>());
        NavigateToMacchineCommand  = new RelayCommand(_ => _navigation.Navigate<MacchineViewModel>());
        NavigateToDisegniCommand   = new RelayCommand(_ => _navigation.Navigate<DisegniViewModel>());
    }

    public ViewModelBase? CurrentViewModel => _navigation.CurrentViewModel;

    public ICommand NavigateToDashboardCommand { get; }
    public ICommand NavigateToClientiCommand   { get; }
    public ICommand NavigateToPiastreCommand   { get; }
    public ICommand NavigateToMacchineCommand  { get; }
    public ICommand NavigateToDisegniCommand   { get; }
}
