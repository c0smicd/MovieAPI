using System.ComponentModel.DataAnnotations;

namespace MovieAPI.Models;

public class Auditorium
{
    [Required]
    public int Id { get; set; }

    [MaxLength(100, ErrorMessage = "Auditorium name cannot exceed 100 characters.")]
    public string AuditoriumName { get; set; } = "";


    // Foreign Model Relations
    [Required]
    public int SeatingPlanId { get; set; }

    public SeatingPlan SeatingPlan { get; set; } = null!;

    public ICollection<Movie> Movies { get; set; } = new List<Movie>();
}