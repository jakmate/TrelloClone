using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

    public DbSet<Board> Boards => Set<Board>();
    public DbSet<Column> Columns => Set<Column>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<User> Users => Set<User>();
    public DbSet<BoardUser> BoardUsers => Set<BoardUser>();
    public DbSet<BoardInvitation> BoardInvitation => Set<BoardInvitation>();
    public DbSet<TaskAssignment> TaskAssignments => Set<TaskAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new BoardConfiguration());
        modelBuilder.ApplyConfiguration(new ColumnConfiguration());
        modelBuilder.ApplyConfiguration(new TaskItemConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new BoardUserConfiguration());
        modelBuilder.ApplyConfiguration(new BoardInvitationConfiguration());
        modelBuilder.ApplyConfiguration(new TaskAssignmentConfiguration());

        // BoardUser composite PK and relationships
        modelBuilder.Entity<BoardUser>()
            .HasKey(bu => new { bu.BoardId, bu.UserId });
        modelBuilder.Entity<BoardUser>()
            .HasOne(bu => bu.Board).WithMany(b => b.BoardUsers).HasForeignKey(bu => bu.BoardId);
        modelBuilder.Entity<BoardUser>()
            .HasOne(bu => bu.User).WithMany(u => u.BoardUsers).HasForeignKey(bu => bu.UserId);

        // TaskAssignment composite PK and relationships
        modelBuilder.Entity<TaskAssignment>()
            .HasKey(ta => new { ta.TaskId, ta.UserId });
        modelBuilder.Entity<TaskAssignment>()
            .HasOne(ta => ta.Task)
            .WithMany(t => t.TaskAssignments)
            .HasForeignKey(ta => ta.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TaskAssignment>()
            .HasOne(ta => ta.User)
            .WithMany()
            .HasForeignKey(ta => ta.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure many-to-many through TaskAssignment
        modelBuilder.Entity<TaskItem>()
            .HasMany(t => t.AssignedUsers)
            .WithMany()
            .UsingEntity<TaskAssignment>();
    }
}