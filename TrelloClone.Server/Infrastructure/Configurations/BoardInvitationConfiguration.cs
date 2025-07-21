using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class BoardInvitationConfiguration : IEntityTypeConfiguration<BoardInvitation>
{
    public void Configure(EntityTypeBuilder<BoardInvitation> builder)
    {
        builder.ToTable("BoardInvitations");
        builder.HasKey(i => i.Id);

        // Relationships
        builder.HasOne(i => i.Board)
            .WithMany(b => b.Invitations)
            .HasForeignKey(i => i.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.InviterUser)
            .WithMany(u => u.SentInvitations)
            .HasForeignKey(i => i.InviterUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.InvitedUser)
            .WithMany(u => u.ReceivedInvitations)
            .HasForeignKey(i => i.InvitedUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}