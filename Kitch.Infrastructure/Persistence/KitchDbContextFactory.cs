using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kitch.Infrastructure.Persistence;

public class KitchDbContextFactory : IDesignTimeDbContextFactory<KitchDbContext>
{
    public KitchDbContext CreateDbContext(string[] args)
    {

        var optionsBuilder = new DbContextOptionsBuilder<KitchDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=KitchDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");

        return new KitchDbContext(optionsBuilder.Options);
    }
}