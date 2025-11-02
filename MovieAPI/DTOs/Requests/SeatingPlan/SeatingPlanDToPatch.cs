using System.ComponentModel.DataAnnotations;

namespace MovieAPI.DTOs.Requests.SeatingPlan;

public class SeatingPlanDToPatch
{
    [MaxLength(50, ErrorMessage = "Name of the seating plan cannot exceed 50 characters.")]
    public string PlanName { get; set; } = "";

    [Required] public string LayoutJson { get; set; } = "";

    [MaxLength(500, ErrorMessage = "Description length cannot exceed 500 characters.")]
    public string Description { get; set; } = "";
}