using System.ComponentModel.DataAnnotations;

namespace MovieAPI.DTOs.Requests;

public class MovieDToCreate
{
    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(255, ErrorMessage = "Title length cannot exceed 255 characters.")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Description length cannot exceed 1000 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Year is required.")]
    public int Year { get; set; }

    [MaxLength(255, ErrorMessage = "Director name length cannot exceed 255 characters.")]
    public string? Director { get; set; }

    [Range(0, 100, ErrorMessage = "Rating must be between 0 and 10.")]
    public double Rating { get; set; }

    [Range(1, 1000, ErrorMessage = "Runtime must be between 1 and 1000 minutes.")]
    public double RuntimeMinutes { get; set; }

    [MaxLength(500, ErrorMessage = "Genre length cannot exceed 500 characters.")]
    public string? Genre { get; set; }

    [MaxLength(500, ErrorMessage = "Poster URL length cannot exceed 500 characters.")]
    [Url(ErrorMessage = "Poster URL must be a valid URL.")]
    public string? PosterUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Related entity references by ID
    public List<int>? AuditoriumIds { get; set; }
}