using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PlateArchive.Data;

public class PlateArchiveDbContextFactory : IDesignTimeDbContextFactory<PlateArchiveDbContext>
{
    public PlateArchiveDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PlateArchiveDbContext>()
            .UseSqlServer("Server=localhost;Database=PlateArchiveDB;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        return new PlateArchiveDbContext(options);
    }
}
