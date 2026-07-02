using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using PlateArchive.Wpf.Behaviors;
using PlateArchive.Wpf.ViewModels;

namespace PlateArchive.Wpf.Views;

public partial class OrdiniVenditaView : UserControl
{
    public OrdiniVenditaView()
    {
        InitializeComponent();

        // Le colonne dati arrivano dalla query (ViewModel.Colonne, valorizzata a fine
        // caricamento): la griglia va rigenerata ogni volta che cambiano.
        DataContextChanged += (_, e) =>
        {
            if (e.OldValue is OrdiniVenditaViewModel vecchio) vecchio.PropertyChanged -= ViewModel_PropertyChanged;
            if (e.NewValue is OrdiniVenditaViewModel nuovo)
            {
                nuovo.PropertyChanged += ViewModel_PropertyChanged;
                GeneraColonne();
            }
        };
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OrdiniVenditaViewModel.Colonne))
            GeneraColonne();
    }

    /// <summary>
    /// Ricrea le colonne dati della griglia in base alle colonne della query
    /// (Db2:QueryRigheOrdineVendita): nome colonna/alias = intestazione, ordine SELECT = ordine
    /// griglia. La colonna template con i pulsanti azione (definita nel XAML) resta ultima.
    /// </summary>
    private void GeneraColonne()
    {
        if (DataContext is not OrdiniVenditaViewModel vm || vm.Colonne.Count == 0) return;

        while (GrigliaOrdini.Columns.Count > 1)
            GrigliaOrdini.Columns.RemoveAt(0);

        for (int i = 0; i < vm.Colonne.Count; i++)
        {
            GrigliaOrdini.Columns.Insert(i, new DataGridTextColumn
            {
                Header  = vm.Colonne[i],
                Binding = new Binding($"Riga.Valori[{i}]"),
                Width   = DataGridLength.Auto,
            });
        }

        // Il ripristino automatico del behavior avviene sul Loaded, quando queste colonne
        // non esistono ancora: va riapplicato ora che ci sono.
        DataGridColumnLayout.Ripristina(GrigliaOrdini, DataGridColumnLayout.GetChiave(GrigliaOrdini)!);
    }

    private void AssociaPiastra_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: RigaOrdineVenditaRow row }) return;
        _ = ApriAssociaPiastraAsync(row);
    }

    private void Riga_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGridRow { DataContext: RigaOrdineVenditaRow row }) return;

        if (row.PiastraTrovata)
            _ = ApriDettaglioPiastraAsync(row);
        else
            _ = ApriAssociaPiastraAsync(row);
    }

    private async Task ApriDettaglioPiastraAsync(RigaOrdineVenditaRow row)
    {
        if (row.Piastra is null) return;

        var vm = App.ServiceProvider.GetRequiredService<PiastraDettaglioViewModel>();
        await vm.InitAsync(row.Piastra.IdPiastra);
        new PiastraDettaglioWindow(vm) { Owner = Window.GetWindow(this) }.ShowDialog();
    }

    private async Task ApriAssociaPiastraAsync(RigaOrdineVenditaRow row)
    {
        if (DataContext is not OrdiniVenditaViewModel viewModel) return;

        var vm = App.ServiceProvider.GetRequiredService<AssociaPiastraOrdineViewModel>();
        await vm.InitAsync(row.Riga.CodiceArticolo);
        new AssociaPiastraOrdineWindow(vm) { Owner = Window.GetWindow(this) }.ShowDialog();

        if (vm.Confermato)
            await viewModel.RicaricaRigaAsync(row);
    }
}
