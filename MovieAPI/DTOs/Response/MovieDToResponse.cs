namespace MovieAPI.DTOs.Response;

public class MovieDToResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public double? Rating { get; set; }
    public string? Genre { get; set; }
    public string? PosterUrl { get; set; }
}