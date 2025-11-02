using System.ComponentModel.DataAnnotations;

namespace MovieAPI.DTOs.Requests.Auditorium;

public class AuditoriumDToCreate
{
    [Required(ErrorMessage = "Id is required.")]
    public int Id { get; set; }

    [MaxLength(100, ErrorMessage = "Auditorium name cannot exceed 100 characters.")]
    public string AuditoriumName { get; set; } = "";

    [Required(ErrorMessage = "SeatingPlanIds are required.")]
    public int SeatingPlanId { get; set; }
}