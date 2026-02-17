using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DogDoor.Api.Data;

public class DogDoorDbContextFactory : IDesignTimeDbContextFactory<DogDoorDbContext>
{
    public DogDoorDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DogDoorDbContext>()
            .UseNpgsql("Host=localhost;Database=dogdoor_design;Username=postgres;Password=postgres")
            .Options;

        return new DogDoorDbContext(options);
    }
}
