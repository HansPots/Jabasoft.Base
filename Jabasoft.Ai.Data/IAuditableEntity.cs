namespace Jabasoft.Ai.Data;

/// <summary>
/// Standing convention across every Jabasoft database: an entity that
/// tracks when it was created and last changed. AiDbContext stamps both
/// automatically in SaveChanges/SaveChangesAsync - implementing entities
/// never set these themselves. Same pattern as Stylebook.Data's
/// IAuditableEntity; kept as its own copy here rather than shared, the
/// same way Stylebook.Data doesn't reference this repo either.
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAtUtc { get; set; }

    DateTime UpdatedAtUtc { get; set; }
}
