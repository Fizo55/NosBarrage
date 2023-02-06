using Microsoft.EntityFrameworkCore;

namespace NosBarrage.Database;

public class NosBarrageContext : DbContext
{
    private readonly DbContextOptions<NosBarrageContext> _options;

    public NosBarrageContext(DbContextOptions<NosBarrageContext> options) : base(options)
    {
        _options = options;
    }
}
