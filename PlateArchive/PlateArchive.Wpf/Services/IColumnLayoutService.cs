using PlateArchive.Wpf.Models;

namespace PlateArchive.Wpf.Services;

public interface IColumnLayoutService
{
    IReadOnlyList<ColonnaLayout>? Carica(string chiave);
    void Salva(string chiave, IEnumerable<ColonnaLayout> colonne);
}
