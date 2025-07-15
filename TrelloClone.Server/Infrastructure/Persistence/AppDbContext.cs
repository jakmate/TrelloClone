using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

    public DbSet<Board> Boards => Set<Board>();
    public DbSet<Column> Columns => Set<Column>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<User> Users => Set<User>();
    public DbSet<BoardUser> BoardUsers => Set<BoardUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new BoardConfiguration());
        modelBuilder.ApplyConfiguration(new ColumnConfiguration());
        modelBuilder.ApplyConfiguration(new TaskItemConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new BoardUserConfiguration());

        // composite PK for join table
        modelBuilder.Entity<BoardUser>()
            .HasKey(bu => new { bu.BoardId, bu.UserId });

        // relationships
        modelBuilder.Entity<BoardUser>()
            .HasOne(bu => bu.Board).WithMany(b => b.BoardUsers).HasForeignKey(bu => bu.BoardId);
        modelBuilder.Entity<BoardUser>()
            .HasOne(bu => bu.User).WithMany(u => u.BoardUsers).HasForeignKey(bu => bu.UserId);
    }
}
