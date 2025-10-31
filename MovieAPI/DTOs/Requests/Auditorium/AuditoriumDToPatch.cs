using System.ComponentModel.DataAnnotations;

namespace MovieAPI.DTOs.Requests.Auditorium;

public class AuditoriumDToPatch
{
    [MaxLength(100, ErrorMessage = "Auditorium name cannot exceed 100 characters.")]
    public string? AuditoriumName { get; set; }
    public int? SeatingPlanId { get; set; }
    public int? MovieId { get; set; }


}