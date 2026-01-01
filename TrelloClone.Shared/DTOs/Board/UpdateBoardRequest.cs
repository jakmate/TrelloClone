using System.ComponentModel.DataAnnotations;

namespace TrelloClone.Shared.DTOs.Board;

public class UpdateBoardRequest
{
    [Required]
    public string? Name { get; set; }
}
