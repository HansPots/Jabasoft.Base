using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Jabasoft.Ai.Data.Entities;

namespace Jabasoft.Ai.Data.Configurations;

public sealed class SearchDocumentConfiguration : IEntityTypeConfiguration<SearchDocument>
{
    public void Configure(EntityTypeBuilder<SearchDocument> builder)
    {
        builder.ToTable("SearchDocuments");
        builder.HasKey(d => d.Id);

        // Eén rij per bestand per project - opnieuw indexeren werkt zo als
        // upsert in plaats van dat er dubbels bijkomen.
        builder.HasIndex(d => new { d.Project, d.FilePath }).IsUnique();

        builder.Property(d => d.Id).HasColumnOrder(0);
        builder.Property(d => d.Project).HasMaxLength(1000).IsRequired().HasColumnOrder(1);
        builder.Property(d => d.FilePath).HasMaxLength(1000).IsRequired().HasColumnOrder(2);
        builder.Property(d => d.Model).HasMaxLength(200).IsRequired().HasColumnOrder(3);
        builder.Property(d => d.Content).IsRequired().HasColumnOrder(4);
        builder.Property(d => d.Embedding).IsRequired().HasColumnOrder(5);
        builder.Property(d => d.CreatedAtUtc).IsRequired().HasColumnOrder(6);
        builder.Property(d => d.UpdatedAtUtc).IsRequired().HasColumnOrder(7);
    }
}
