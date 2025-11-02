using System.ComponentModel.DataAnnotations;

namespace MovieAPI.DTOs.Requests.Auditorium;

public class UpdateSeatingPlanRequest
{
    [Required]
    public int SeatingPlanId { get; set; }
}
