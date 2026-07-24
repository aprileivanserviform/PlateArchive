using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PlateArchive.Wpf.ViewModels;

/// <summary>
/// Classe base di tutti i ViewModel dell'applicazione.
/// Implementa <see cref="INotifyPropertyChanged"/> per il binding WPF:
/// ogni volta che una proprietà cambia, la View si aggiorna automaticamente.
/// <para>
/// Pattern d'uso:
/// <code>
/// public string Nome
/// {
///     get => _nome;
///     set => SetField(ref _nome, value);  // notifica la View
/// }
/// </code>
/// </para>
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Override per eseguire il caricamento dati asincrono al momento della navigazione.
    /// NavigationService chiama questo metodo dopo aver impostato CurrentViewModel,
    /// così la View può mostrare uno stato di caricamento mentre i dati arrivano dal DB.
    /// </summary>
    public virtual Task OnNavigatedAsync() => Task.CompletedTask;

    /// <summary>
    /// Solleva l'evento PropertyChanged per la proprietà specificata.
    /// [CallerMemberName] riempie automaticamente il nome della proprietà
    /// quando chiamato dall'interno della proprietà stessa.
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>
    /// Aggiorna il campo backing e notifica la View solo se il valore è effettivamente cambiato.
    /// Ritorna true se il valore è cambiato (utile per eseguire logica aggiuntiva nel setter).
    /// </summary>
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    /// <summary>
    /// Esegue un'operazione sul database mostrando un messaggio contestuale se fallisce.
    /// Restituisce <c>true</c> se l'operazione è riuscita, <c>false</c> altrimenti: il chiamante
    /// deve interrompersi in caso di errore, senza chiudere il form, così l'utente può correggere
    /// il dato e riprovare senza perdere quanto inserito.
    /// <para>
    /// <paramref name="operazione"/> descrive cosa si stava facendo, all'infinito e con il
    /// riferimento all'elemento: es. <c>"salvare la macchina 'SPRINTERA 106'"</c>. Il messaggio
    /// risultante è "Impossibile {operazione}." seguito dalla causa.
    /// </para>
    /// <example>
    /// <code>
    /// if (!await ProvaAsync(() => _repo.AddAsync(nuova), $"salvare la macchina '{nuova.CodiceMacchina}'"))
    ///     return;
    /// </code>
    /// </example>
    /// </summary>
    protected static async Task<bool> ProvaAsync(Func<Task> azione, string operazione)
    {
        try
        {
            await azione();
            return true;
        }
        catch (Exception ex)
        {
            var causa = App.CausaErrore(ex);
            MessageBox.Show(
                causa is not null
                    ? $"Impossibile {operazione}.\n\n{causa}"
                    : $"Impossibile {operazione}.\n\nErrore imprevisto: {ex.Message}",
                "Operazione non riuscita",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }
    }
}
