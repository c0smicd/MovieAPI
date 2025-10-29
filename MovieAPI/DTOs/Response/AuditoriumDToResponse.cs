namespace MovieAPI.DTOs.Response;

public class AuditoriumDToResponse
{
    public int Id { get; set; }
    public string AuditoriumName { get; set; } = string.Empty;

    public SeatingPlanDToResponse SeatingPlan { get; set; } = new();
    public List<MovieDToResponse>? Movies { get; set; }
}