using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class TaskAssignmentConfiguration : IEntityTypeConfiguration<TaskAssignment>
{
    public void Configure(EntityTypeBuilder<TaskAssignment> builder)
    {
        builder.ToTable("TaskAssignments");
        builder.HasKey(ta => new { ta.TaskId, ta.UserId });

        builder.Property(ta => ta.AssignedAt)
               .IsRequired();

        builder.HasOne(ta => ta.Task)
               .WithMany(t => t.TaskAssignments)
               .HasForeignKey(ta => ta.TaskId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ta => ta.User)
               .WithMany()
               .HasForeignKey(ta => ta.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}