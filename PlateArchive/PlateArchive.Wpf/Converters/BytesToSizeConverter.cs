using System.Globalization;
using System.Windows.Data;

namespace PlateArchive.Wpf.Converters;

/// <summary>
/// Converte un valore long (byte) in stringa leggibile (B / KB / MB).
/// Es: 1536 → "1.5 KB", 2097152 → "2.0 MB"
/// </summary>
public class BytesToSizeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not long bytes) return string.Empty;
        return bytes switch
        {
            < 1024            => $"{bytes} B",
            < 1024 * 1024     => $"{bytes / 1024.0:F1} KB",
            _                 => $"{bytes / (1024.0 * 1024):F1} MB"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
