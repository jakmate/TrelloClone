using System.ComponentModel.DataAnnotations;

namespace TrelloClone.Shared.DTOs;

public class UpdateBoardRequest
{
    [Required]
    public string Name { get; set; }
}