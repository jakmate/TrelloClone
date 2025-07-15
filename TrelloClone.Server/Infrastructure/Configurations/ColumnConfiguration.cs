using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ColumnConfiguration : IEntityTypeConfiguration<Column>
{
    public void Configure(EntityTypeBuilder<Column> builder)
    {
        builder.ToTable("Columns");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title)
               .IsRequired()
               .HasMaxLength(64);

        builder.Property(c => c.Position)
               .IsRequired();

        // One-to-many Board
        builder.HasOne(c => c.Board)
               .WithMany(b => b.Columns)
               .HasForeignKey(c => c.BoardId)
               .OnDelete(DeleteBehavior.Cascade);

        // One-to-many Tasks
        builder.HasMany(c => c.Tasks)
               .WithOne(t => t.Column)
               .HasForeignKey(t => t.ColumnId);
    }
}