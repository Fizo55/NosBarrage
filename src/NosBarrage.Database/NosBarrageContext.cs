using Microsoft.EntityFrameworkCore;
using NosBarrage.Shared.Entities;

namespace NosBarrage.Database;

public class NosBarrageContext : DbContext
{
    private readonly DbContextOptions<NosBarrageContext> _options;

    public NosBarrageContext(DbContextOptions<NosBarrageContext> options) : base(options)
    {
        _options = options;
    }

    public DbSet<AccountEntity> Account { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountEntity>()
            .HasKey(e => e.AccountId);
    }
}
