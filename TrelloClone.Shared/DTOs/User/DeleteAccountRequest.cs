using System.ComponentModel.DataAnnotations;

namespace TrelloClone.Shared.DTOs.User;

public class DeleteAccountRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;
}
