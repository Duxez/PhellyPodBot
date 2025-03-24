using HomeGameBot.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;

namespace HomeGameBot.Persistence;

public interface IHomeGameContext
{
    DbSet<Pod> Pods { get; }
    DbSet<User> Users { get; }
    Task<int> CommitAsync(CancellationToken ct = default);
}

public sealed class HomeGameContext : DbContext, IHomeGameContext
{
    private readonly ILogger<HomeGameContext> _logger;

    public DbSet<Pod> Pods => Set<Pod>();
    public DbSet<User> Users => Set<User>();

    public HomeGameContext(
        DbContextOptions<HomeGameContext> options,
        ILogger<HomeGameContext> logger) : base(options)
    {
        _logger = logger;
        Database.EnsureCreated();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(HomeGameContext).Assembly);
        
        // Temporal table configuration for audit trails
        modelBuilder.Entity<Pod>()
            .ToTable(tb => tb.IsTemporal());
        modelBuilder.Entity<User>()
            .ToTable(tb => tb.IsTemporal());
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken ct = default)
    {
        try
        {
            UpdateAuditableEntities();
            return await base.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update exception occurred");
            throw new DataCommitException("Failed to persist game state", ex);
        }
    }

    private void UpdateAuditableEntities()
    {
        var entries = ChangeTracker.Entries<IAuditable>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
        }
    }

    public async Task<int> CommitAsync(CancellationToken ct = default) 
        => await SaveChangesAsync(ct);
}

// Configuration class for Entity mappings
internal sealed class PodConfiguration : IEntityTypeConfiguration<Pod>
{
    public void Configure(EntityTypeBuilder<Pod> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(new UlidToStringConverter());
        
        builder.HasIndex(p => p.Code)
            .IsUnique()
            .HasDatabaseName("IX_Pod_Code");
        
        builder.HasMany(p => p.Members)
            .WithOne(u => u.Pod)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// Extension method for service configuration
public static class PersistenceExtensions
{
    public static IServiceCollection AddGamePersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("GameDatabase");
        
        services.AddDbContext<IHomeGameContext, HomeGameContext>(options =>
        {
            options.UseSqlite(
                connectionString,
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(
                        typeof(HomeGameContext).Assembly.FullName);
                    sqlOptions.CommandTimeout(30);
                    sqlOptions.UseQuerySplittingBehavior(
                        QuerySplittingBehavior.SplitQuery);
                });
            
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging()
                    .EnableDetailedErrors()
                    .LogTo(Console.WriteLine, LogLevel.Information);
            }
        });
        
        return services;
    }
}

// Custom exception for domain error handling
public class DataCommitException : Exception
{
    public DataCommitException(string message, Exception inner)
        : base(message, inner) { }
}
