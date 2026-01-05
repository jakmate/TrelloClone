using System.ComponentModel.DataAnnotations;

namespace TrelloClone.Shared.DTOs.User;

public class UpdateUserRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    [Required]
    public string CurrentPassword { get; set; } = string.Empty;
}
