using Microsoft.EntityFrameworkCore;
using NosBarrage.Shared.Entities;

namespace NosBarrage.Database;

public class NosBarrageContext : DbContext
{

    public NosBarrageContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<AccountEntity> Account { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountEntity>()
            .HasKey(e => e.AccountId);
    }
}
