namespace MovieAPI.DTOs.Requests;

public class MovieDToPatch
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int Year { get; set; }
    public string? Director { get; set; }
    public double Rating { get; set; }
    public double RuntimeMinutes { get; set; }
    public string? Genre { get; set; }
    public string? PosterUrl { get; set; }

    // Related entity references by ID
    public List<int>? AuditoriumIds { get; set; }

}