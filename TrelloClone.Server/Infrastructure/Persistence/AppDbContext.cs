using Microsoft.EntityFrameworkCore;

using TrelloClone.Server.Domain.Entities;
using TrelloClone.Server.Infrastructure.Configurations;

namespace TrelloClone.Server.Infrastructure.Persistance;

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

        // BoardUser composite PK and relationships
        modelBuilder.Entity<BoardUser>()
            .HasKey(bu => new { bu.BoardId, bu.UserId });
        modelBuilder.Entity<BoardUser>()
            .HasOne(bu => bu.Board).WithMany(b => b.BoardUsers).HasForeignKey(bu => bu.BoardId);
        modelBuilder.Entity<BoardUser>()
            .HasOne(bu => bu.User).WithMany(u => u.BoardUsers).HasForeignKey(bu => bu.UserId);
    }
}
