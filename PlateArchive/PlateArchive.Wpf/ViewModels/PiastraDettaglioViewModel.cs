using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Wpf.Commands;
using PlateArchive.Wpf.Services;

namespace PlateArchive.Wpf.ViewModels;

/// <summary>
/// ViewModel per la finestra popup "Caratteristiche piastra" — vista di sola lettura
/// aperta dalla lista Piastre del dettaglio cliente (icona "Apri caratteristiche").
/// Ricarica la piastra tramite <see cref="IPiastraRepository.GetByIdAsync"/> per avere
/// sempre Categoria/Formato/Disegno popolati, indipendentemente da cosa sia stato
/// incluso nella query di origine (es. ClientePiastraRepository non include Categoria/Formato).
/// </summary>
public class PiastraDettaglioViewModel : ViewModelBase
{
    private readonly IPiastraRepository _piastreRepo;

    private Piastra?      _piastra;
    private string?       _descrizioneArticoloGestionale;
    private BitmapSource? _anteprimaDisegno;
    private bool          _isCaricamentoAnteprima;
    private string?       _erroreDisegno;

    public PiastraDettaglioViewModel(IPiastraRepository piastreRepo)
    {
        _piastreRepo = piastreRepo;

        AprirDisegnoCommand = new RelayCommand(_ => AprirDisegno(), _ => Piastra?.Disegno is not null);
    }

    public Piastra? Piastra
    {
        get => _piastra;
        private set
        {
            if (SetField(ref _piastra, value))
            {
                OnPropertyChanged(nameof(IsDisegnoPresente));
                OnPropertyChanged(nameof(IsDisegnoAssente));
                OnPropertyChanged(nameof(IsNoAnteprima));
            }
        }
    }

    public bool IsDisegnoPresente => Piastra?.Disegno is not null;
    public bool IsDisegnoAssente  => Piastra is not null && Piastra.Disegno is null;

    /// <summary>
    /// Descrizione estesa dell'articolo (DESCR_ESTESA), disponibile solo quando la finestra
    /// viene aperta da una riga ordine: è un dato live del gestionale, non salvato in locale.
    /// </summary>
    public string? DescrizioneArticoloGestionale
    {
        get => _descrizioneArticoloGestionale;
        private set
        {
            if (SetField(ref _descrizioneArticoloGestionale, value))
                OnPropertyChanged(nameof(IsDescrizioneArticoloVisible));
        }
    }

    public bool IsDescrizioneArticoloVisible => !string.IsNullOrEmpty(_descrizioneArticoloGestionale);

    public BitmapSource? AnteprimaDisegno
    {
        get => _anteprimaDisegno;
        private set
        {
            if (SetField(ref _anteprimaDisegno, value))
            {
                OnPropertyChanged(nameof(IsAnteprimaVisible));
                OnPropertyChanged(nameof(IsNoAnteprima));
            }
        }
    }

    public bool IsAnteprimaVisible => _anteprimaDisegno is not null;

    public bool IsCaricamentoAnteprima
    {
        get => _isCaricamentoAnteprima;
        private set { if (SetField(ref _isCaricamentoAnteprima, value)) OnPropertyChanged(nameof(IsNoAnteprima)); }
    }

    public bool IsNoAnteprima => IsDisegnoPresente && !IsAnteprimaVisible && !IsCaricamentoAnteprima;

    public string? ErroreDisegno
    {
        get => _erroreDisegno;
        set { if (SetField(ref _erroreDisegno, value)) OnPropertyChanged(nameof(IsErroreDisegnoVisible)); }
    }

    public bool IsErroreDisegnoVisible => !string.IsNullOrEmpty(_erroreDisegno);

    public ICommand AprirDisegnoCommand { get; }

    public async Task InitAsync(int idPiastra, string? descrizioneArticoloGestionale = null)
    {
        Piastra = await _piastreRepo.GetByIdAsync(idPiastra);
        DescrizioneArticoloGestionale = descrizioneArticoloGestionale;

        if (Piastra?.Disegno is not null)
        {
            IsCaricamentoAnteprima = true;
            try   { AnteprimaDisegno = await DwgThumbnailReader.EstraiAnteprimaAsync(Piastra.Disegno.PercorsoFile); }
            finally { IsCaricamentoAnteprima = false; }
        }
    }

    private void AprirDisegno()
    {
        var percorso = Piastra?.Disegno?.PercorsoFile;
        if (string.IsNullOrEmpty(percorso)) return;

        ErroreDisegno = null;

        if (!File.Exists(percorso))
        {
            ErroreDisegno = $"File non trovato: {percorso}";
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(percorso) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            ErroreDisegno = $"Impossibile aprire il file: {ex.Message}";
        }
    }
}
