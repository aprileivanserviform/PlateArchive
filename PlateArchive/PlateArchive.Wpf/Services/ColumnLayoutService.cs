using System.IO;
using System.Text.Json;
using PlateArchive.Wpf.Models;

namespace PlateArchive.Wpf.Services;

public sealed class ColumnLayoutService : IColumnLayoutService
{
    private static readonly string _percorso = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PlateArchive",
        "ui-settings.json");

    private static readonly JsonSerializerOptions _jsonOpt = new() { WriteIndented = true };

    private Dictionary<string, List<ColonnaLayout>> _cache = [];
    private bool _caricato;

    public IReadOnlyList<ColonnaLayout>? Carica(string chiave)
    {
        EnsureCaricato();
        return _cache.TryGetValue(chiave, out var layout) ? layout : null;
    }

    public void Salva(string chiave, IEnumerable<ColonnaLayout> colonne)
    {
        EnsureCaricato();
        _cache[chiave] = [.. colonne];

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_percorso)!);
            File.WriteAllText(_percorso, JsonSerializer.Serialize(_cache, _jsonOpt));
        }
        catch { /* ignora errori di scrittura */ }
    }

    private void EnsureCaricato()
    {
        if (_caricato) return;
        _caricato = true;

        try
        {
            if (File.Exists(_percorso))
            {
                var json = File.ReadAllText(_percorso);
                _cache = JsonSerializer.Deserialize<Dictionary<string, List<ColonnaLayout>>>(json) ?? [];
            }
        }
        catch { _cache = []; }
    }
}
