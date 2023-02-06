using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NosBarrage.Database
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<NosBarrageContext>
    {
        public NosBarrageContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<NosBarrageContext>();
            // todo : remove hardcoded string
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres");
            return new NosBarrageContext(optionsBuilder.Options);
        }
    }
}
