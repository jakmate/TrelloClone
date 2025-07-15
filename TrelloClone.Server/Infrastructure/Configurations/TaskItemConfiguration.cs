using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
               .IsRequired()
               .HasMaxLength(64);

        builder.Property(t => t.Priority)
               .IsRequired();

        // AssignedUser (optional)
        builder.HasOne(t => t.AssignedUser)
               .WithMany(u => u.AssignedTasks)
               .HasForeignKey(t => t.AssignedUserId)
               .OnDelete(DeleteBehavior.SetNull);

        // Column (required)
        builder.HasOne(t => t.Column)
               .WithMany(c => c.Tasks)
               .HasForeignKey(t => t.ColumnId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}