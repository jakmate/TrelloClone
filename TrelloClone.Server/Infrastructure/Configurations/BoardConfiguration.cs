using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class BoardConfiguration : IEntityTypeConfiguration<Board>
{
       public void Configure(EntityTypeBuilder<Board> builder)
       {
              builder.ToTable("Boards");
              builder.HasKey(b => b.Id);
              builder.Property(b => b.Name)
                     .IsRequired()
                     .HasMaxLength(32);

              // one-to-many Columns
              builder.HasMany(b => b.Columns)
                     .WithOne(c => c.Board)
                     .HasForeignKey(c => c.BoardId)
                     .OnDelete(DeleteBehavior.Cascade);

              // many-to-many via join-entity
              builder.HasMany(b => b.BoardUsers)
                     .WithOne(bu => bu.Board)
                     .HasForeignKey(bu => bu.BoardId);
       }
}