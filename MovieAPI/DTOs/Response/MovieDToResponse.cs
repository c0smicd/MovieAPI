namespace MovieAPI.DTOs.Response;

public class MovieDToResponse
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public double? Rating { get; init; }
    public string? Genre { get; init; }
    public string? PosterUrl { get; init; }
}