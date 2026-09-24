using Microsoft.EntityFrameworkCore;
using Jabasoft.Ai.Data.Configurations;
using Jabasoft.Ai.Data.Entities;

namespace Jabasoft.Ai.Data;

/// <summary>
/// De database voor AI-gerelateerde gegevens die geen applicatie zelf
/// bijhoudt - eigen database (JabasoftAi), los van JabasoftBase
/// (tokenverbruik, via rechtstreeks SqlClient) en JabasoftStylebook
/// (het thema, via Stylebook.Data). Begint met de zoekindex voor
/// semantisch zoeken; verdere AI-opslag hoort hier ook thuis zodra dat
/// nodig is.
/// </summary>
public sealed class AiDbContext(DbContextOptions<AiDbContext> options) : DbContext(options)
{
    public DbSet<SearchDocument> SearchDocuments => Set<SearchDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new SearchDocumentConfiguration());
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.UpdatedAtUtc = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
            }
        }
    }
}
