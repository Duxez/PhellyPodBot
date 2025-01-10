using Microsoft.EntityFrameworkCore;

namespace HomeGameBot.Data;

internal sealed class HomeGameContext : DbContext
{
    public DbSet<Pod> Pods { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        
        optionsBuilder.UseSqlite("Data Source='data/homegame.db'");
    }
}