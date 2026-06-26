using System.Windows.Input;

namespace PlateArchive.Wpf.Commands;

/// <summary>
/// Implementazione generica di ICommand basata su delegati.
/// Usata da tutti i ViewModel per esporre comandi alla View senza code-behind.
/// <para>
/// CanExecuteChanged è collegato a CommandManager.RequerySuggested: WPF ri-valuta
/// automaticamente CanExecute dopo ogni interazione UI (click, input), aggiornando
/// lo stato Enabled/Disabled dei controlli legati al comando.
/// </para>
/// </summary>
public class RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null) : ICommand
{
    // RequerySuggested è un evento globale di WPF che si scatena dopo ogni UIElement.Focus
    // e dopo ogni CommandManager.InvalidateRequerySuggested() chiamato esplicitamente.
    public event EventHandler? CanExecuteChanged
    {
        add    => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    // Se canExecute non è fornito, il comando è sempre abilitato.
    public bool CanExecute(object? p) => canExecute?.Invoke(p) ?? true;
    public void Execute(object? p) => execute(p);

    /// <summary>Forza WPF a rivalutare CanExecute su tutti i comandi registrati.</summary>
    public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
}
