using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PlateArchive.Data;

public class PlateArchiveDbContextFactory : IDesignTimeDbContextFactory<PlateArchiveDbContext>
{
    public PlateArchiveDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PlateArchiveDbContext>()
            .UseSqlite("Data Source=platearchive.db")
            .Options;
        return new PlateArchiveDbContext(options);
    }
}
