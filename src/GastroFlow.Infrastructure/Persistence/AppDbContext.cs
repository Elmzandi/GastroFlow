using System.Reflection;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    private readonly ITenantContext _tenant;
    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant) : base(options)
    {
        _tenant = tenant;
    }
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Configure your entity mappings here
        // GLOBAL TENANT FILTER
        // This loops every entity in the model.
        // If it inherits BaseEntity, it gets an automatic WHERE clause:
        //   WHERE "RestaurantId" = @currentRestaurantId
        // You write this once. Every query on every entity is tenant-scoped.
        // Restaurant A CANNOT see Restaurant B's data. Ever.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(SetTenantFilter),
                        BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(this, [modelBuilder]);
            }
        }

        modelBuilder.Entity<Restaurant>()
            .HasIndex(r => r.Email)
            .IsUnique();
    }
    private void SetTenantFilter<T>(ModelBuilder modelBuilder) where T : BaseEntity
    {
        modelBuilder.Entity<T>()
            .HasQueryFilter(e => e.RestaurantId == _tenant.RestaurantId);
    }

    // Auto-set UpdatedAt on every save — you never forget to set it manually
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;

        return base.SaveChangesAsync(ct);
    }
}