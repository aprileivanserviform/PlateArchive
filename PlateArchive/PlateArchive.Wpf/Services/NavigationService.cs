using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using PlateArchive.Wpf.ViewModels;

namespace PlateArchive.Wpf.Services;

public class NavigationService(IServiceScopeFactory scopeFactory) : INotifyPropertyChanged
{
    private IServiceScope? _currentScope;
    private ViewModelBase? _currentViewModel;

    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        private set { _currentViewModel = value; OnPropertyChanged(); }
    }

    public void Navigate<TViewModel>() where TViewModel : ViewModelBase
    {
        _currentScope?.Dispose();
        _currentScope = scopeFactory.CreateScope();
        CurrentViewModel = _currentScope.ServiceProvider.GetRequiredService<TViewModel>();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
