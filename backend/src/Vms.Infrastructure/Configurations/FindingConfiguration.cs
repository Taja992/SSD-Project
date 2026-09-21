using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vms.Domain.Entities;

namespace Vms.Infrastructure.Configurations;

public class FindingConfiguration : IEntityTypeConfiguration<Finding>
{
    public void Configure(EntityTypeBuilder<Finding> builder)
    {
        builder.HasKey(f => f.Id);

        // String length caps are a SECURITY control, not tidiness: they bound
        // what an unauthenticated reporter can push into the database via the
        // public form in Step 4 (T-ER-06, resource exhaustion). Without them
        // PostgreSQL accepts unbounded text.
        builder.Property(f => f.Title).HasMaxLength(200).IsRequired();
        builder.Property(f => f.Description).HasMaxLength(10_000).IsRequired();
        builder.Property(f => f.AffectedAsset).HasMaxLength(200).IsRequired();
        builder.Property(f => f.AssigneeUsername).HasMaxLength(100);
        builder.Property(f => f.CreatedByUsername).HasMaxLength(100);
        builder.Property(f => f.ReporterEmail).HasMaxLength(320); // RFC 5321 max

        // Enums as STRINGS. Stored as integers, reordering the enum silently
        // changes the meaning of every existing row — a real integrity hazard
        // in a column whose whole job is recording severity.
        builder.Property(f => f.Severity).HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.Source).HasConversion<string>().HasMaxLength(20);

        // Indexes for FIND-05 (filter by status, severity, assigned owner).
        builder.HasIndex(f => f.Status);
        builder.HasIndex(f => f.Severity);
        builder.HasIndex(f => f.AssigneeUsername);
    }
}
