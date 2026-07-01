using System.Globalization;
using System.Windows.Data;
using PlateArchive.Wpf.ViewModels;

namespace PlateArchive.Wpf.Converters;

/// <summary>
/// Converte un valore enum FiltroColonnaOp in bool e viceversa.
/// Usato per i RadioButton nel popup di filtro colonna:
///   IsChecked="{Binding Operatore, Converter={StaticResource EnumToBool}, ConverterParameter=Contiene}"
/// Convert:    Operatore == ConverterParameter  →  true
/// ConvertBack: true  →  imposta Operatore = ConverterParameter (come FiltroColonnaOp)
///              false →  Binding.DoNothing (non fa nulla quando il RadioButton viene de-selezionato)
/// </summary>
public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is FiltroColonnaOp op && parameter is string s && op.ToString() == s;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true && parameter is string s && Enum.TryParse<FiltroColonnaOp>(s, out var op))
            return op;
        return Binding.DoNothing;
    }
}
