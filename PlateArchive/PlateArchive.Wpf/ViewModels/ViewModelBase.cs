using System.ComponentModel;
using System.Runtime.CompilerServices;

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
}
