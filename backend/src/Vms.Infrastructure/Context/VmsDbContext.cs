using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Vms.Domain.Entities;
using Vms.Domain.Entities.Interfaces;
using Vms.Infrastructure.Security;

namespace Vms.Infrastructure.Context;

public class VmsDbContext(DbContextOptions<VmsDbContext> options, IFieldEncryptor encryptor)
    : DbContext(options)
{
    public DbSet<Finding> Findings => Set<Finding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Picks up every IEntityTypeConfiguration<T> in this assembly. Add a
        // new configuration file and it is wired up — no edit needed here,
        // so a new entity cannot arrive with its constraints forgotten.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VmsDbContext).Assembly);

        // FIND-11: encrypted at rest. EF encrypts on save and decrypts on read,
        // so the rest of the code only ever sees plain text. The database - and
        // anyone who steals a backup of it - only ever sees ciphertext.
        modelBuilder.Entity<Finding>(finding =>
        {
            finding.Property(f => f.Description)
                   .HasConversion(Encrypted(encryptor, "Finding.Description"));
            finding.Property(f => f.ReporterEmail)
                   .HasConversion(Encrypted(encryptor, "Finding.ReporterEmail"));
        });
    }

    // Static, so the converter captures only the encryptor - not this context.
    // Returns the non-generic base type so one helper fits both string
    // (Description) and string? (ReporterEmail) without a nullability warning.
    private static ValueConverter Encrypted(IFieldEncryptor encryptor, string purpose) =>
        new ValueConverter<string, string>(plain => encryptor.Encrypt(plain, purpose),
            stored => encryptor.Decrypt(stored, purpose));

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
