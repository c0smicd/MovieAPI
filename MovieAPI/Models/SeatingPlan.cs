using System.ComponentModel.DataAnnotations;

namespace MovieAPI.Models;

public class SeatingPlan
{
    [Required] public int Id { get; set; }

    [MaxLength(50, ErrorMessage = "Name of the seating plan cannot exceed 50 characters.")]
    public string PlanName { get; set; } = "";

    [Required]
    public string LayoutJson { get; set; } = "";

    [MaxLength(500, ErrorMessage = "Description length cannot exceed 500 characters.")]
    public string Description { get; set; } = "";

    // Foreign Model Relations
    public int? AuditoriumId { get; set; }

    public Auditorium? Auditorium { get; set; }
}