using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TrelloClone.Server.Domain.Entities;

namespace TrelloClone.Server.Infrastructure.Configurations;

public class BoardUserConfiguration : IEntityTypeConfiguration<BoardUser>
{
    public void Configure(EntityTypeBuilder<BoardUser> builder)
    {
        builder.ToTable("BoardUsers");
        builder.HasKey(bu => new { bu.BoardId, bu.UserId });
        builder.Property(bu => bu.PermissionLevel)
            .IsRequired()
            .HasConversion<string>();
    }
}
