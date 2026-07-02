using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using PlateArchive.Wpf.Models;
using PlateArchive.Wpf.Services;

namespace PlateArchive.Wpf.Behaviors;

/// <summary>
/// Attached behavior che salva e ripristina larghezza e ordine delle colonne di un DataGrid.
/// Uso XAML:  behaviors:DataGridColumnLayout.Chiave="NomeVista.NomeGriglia"
/// Il layout viene salvato in %AppData%\PlateArchive\ui-settings.json.
/// </summary>
public static class DataGridColumnLayout
{
    public static readonly DependencyProperty ChiaveProperty =
        DependencyProperty.RegisterAttached(
            "Chiave",
            typeof(string),
            typeof(DataGridColumnLayout),
            new PropertyMetadata(null, OnChiaveChanged));

    public static void SetChiave(DataGrid d, string? value) => d.SetValue(ChiaveProperty, value);
    public static string? GetChiave(DataGrid d)             => (string?)d.GetValue(ChiaveProperty);

    private static void OnChiaveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid || e.NewValue is not string chiave) return;

        grid.Loaded += (_, _) => RipristinaLayout(grid, chiave);

        // Width: salva quando l'utente rilascia il mouse (fine del trascinamento).
        grid.AddHandler(UIElement.MouseLeftButtonUpEvent,
            new MouseButtonEventHandler((_, _) => SalvaLayout(grid, chiave)),
            handledEventsToo: true);

        // Ordine: salva quando l'utente sposta una colonna.
        grid.ColumnReordered += (_, _) => SalvaLayout(grid, chiave);
    }

    /// <summary>
    /// Riapplica il layout salvato. Da chiamare quando le colonne vengono (ri)generate da
    /// codice DOPO il Loaded della griglia (es. OrdiniVenditaView, colonne guidate dalla query):
    /// il ripristino automatico su Loaded in quel caso non trova ancora le colonne.
    /// </summary>
    public static void Ripristina(DataGrid grid, string chiave) => RipristinaLayout(grid, chiave);

    private static void RipristinaLayout(DataGrid grid, string chiave)
    {
        var svc   = App.ServiceProvider.GetService<IColumnLayoutService>();
        var saved = svc?.Carica(chiave);
        if (saved is null || saved.Count == 0) return;

        // 1. Ripristina le larghezze per colonne trovate nel file.
        //    Le colonne non ridimensionabili (es. colonna azioni) mantengono la larghezza
        //    dichiarata nel XAML: una larghezza salvata in passato non deve sovrascriverla.
        foreach (var col in grid.Columns)
        {
            if (!col.CanUserResize) continue;

            var entry = saved.FirstOrDefault(s => s.Key == GetColKey(col));
            if (entry is not null)
                col.Width = new DataGridLength(entry.Width);
        }

        // 2. Ripristina l'ordine impostando DisplayIndex in ordine crescente
        //    (evita conflitti di indice durante l'assegnazione).
        var inOrdine = saved
            .OrderBy(s => s.DisplayIndex)
            .Select(s => grid.Columns.FirstOrDefault(c => GetColKey(c) == s.Key))
            .Where(c => c is not null)
            .ToList();

        for (int i = 0; i < inOrdine.Count; i++)
            inOrdine[i]!.DisplayIndex = i;

        // 3. Le colonne non riordinabili (es. colonna azioni) devono restare alla posizione
        //    dichiarata nella collezione: la riassegnazione sopra può averle spinte altrove
        //    quando il file salvato non le contiene (o le contiene con una chiave posizionale
        //    ormai stantia, tipo "__col_7").
        for (int i = 0; i < grid.Columns.Count; i++)
        {
            if (!grid.Columns[i].CanUserReorder)
                grid.Columns[i].DisplayIndex = i;
        }
    }

    private static void SalvaLayout(DataGrid grid, string chiave)
    {
        var svc = App.ServiceProvider.GetService<IColumnLayoutService>();
        if (svc is null) return;

        svc.Salva(chiave, grid.Columns.Select(col => new ColonnaLayout
        {
            Key          = GetColKey(col),
            Width        = col.ActualWidth,
            DisplayIndex = col.DisplayIndex,
        }));
    }

    // Chiave univoca per la colonna:
    // 1. SortMemberPath — usato dalle colonne con HeaderTemplate FiltroColonna (PiastreView)
    // 2. Header string  — usato da tutte le altre colonne
    // 3. Fallback posizionale
    private static string GetColKey(DataGridColumn col)
    {
        if (!string.IsNullOrEmpty(col.SortMemberPath))
            return col.SortMemberPath;

        if (col.Header is string h && !string.IsNullOrEmpty(h))
            return h;

        return $"__col_{col.DisplayIndex}";
    }
}
