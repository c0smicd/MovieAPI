namespace MovieAPI.DTOs.Response;

public class SeatingPlanDToResponse
{
    public int Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LayoutJson { get; set; } = string.Empty;

}