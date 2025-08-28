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

        builder.Property(t => t.Position)
               .IsRequired();

        // Column relationship
        builder.HasOne(t => t.Column)
               .WithMany(c => c.Tasks)
               .HasForeignKey(t => t.ColumnId)
               .OnDelete(DeleteBehavior.Cascade);

        // Configure the many-to-many relationship through TaskAssignment
        builder.HasMany(t => t.AssignedUsers)
               .WithMany()
               .UsingEntity<TaskAssignment>(
                   "TaskAssignments",
                   l => l.HasOne(ta => ta.User)
                         .WithMany(u => u.TaskAssignments)
                         .HasForeignKey(ta => ta.UserId)
                         .OnDelete(DeleteBehavior.Cascade),
                   r => r.HasOne(ta => ta.Task)
                         .WithMany(t => t.TaskAssignments)
                         .HasForeignKey(ta => ta.TaskId)
                         .OnDelete(DeleteBehavior.Cascade),
                   j =>
                   {
                       j.HasKey(ta => new { ta.TaskId, ta.UserId });
                       j.Property(ta => ta.AssignedAt)
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");
                   });
    }
}
