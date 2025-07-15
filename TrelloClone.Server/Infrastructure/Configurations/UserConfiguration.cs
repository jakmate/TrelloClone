using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.UserName)
               .IsRequired()
               .HasMaxLength(32);

        builder.Property(u => u.Email)
               .IsRequired()
               .HasMaxLength(64);

        builder.Property(u => u.PasswordHash)
               .IsRequired();

        builder.HasIndex(u => u.Email)
               .IsUnique();

        // Many-to-many with Board through BoardUser
        builder.HasMany(u => u.BoardUsers)
               .WithOne(bu => bu.User)
               .HasForeignKey(bu => bu.UserId);

        // One-to-many with Tasks (assigned)
        builder.HasMany(u => u.AssignedTasks)
               .WithOne(t => t.AssignedUser!)
               .HasForeignKey(t => t.AssignedUserId);
    }
}