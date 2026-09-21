using Microsoft.EntityFrameworkCore;
using Vms.Domain.Entities;
using Vms.Domain.Entities.Interfaces;

namespace Vms.Infrastructure.Context;

public class VmsDbContext(DbContextOptions<VmsDbContext> options) : DbContext(options)
{
    public DbSet<Finding> Findings => Set<Finding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Picks up every IEntityTypeConfiguration<T> in this assembly. Add a
        // new configuration file and it is wired up — no edit needed here,
        // so a new entity cannot arrive with its constraints forgotten.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VmsDbContext).Assembly);
    }

    /// <summary>
    /// ITrackable timestamps are set HERE, never by a caller. A client-supplied
    /// CreatedAt would be unverifiable, and audit evidence must not be.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<ITrackable>())
        {
            if (entry.State == EntityState.Added) entry.Entity.CreatedAt = now;
            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }
        return base.SaveChangesAsync(ct);
    }
}
