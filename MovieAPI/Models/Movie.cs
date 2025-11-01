using System.ComponentModel.DataAnnotations;

namespace MovieAPI.Models;

public class Movie
{   
    [Required]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255, ErrorMessage = "Title length cannot exceed 255 characters.")]
    public string Title { get; set; } = "";
    
    [MaxLength(1000, ErrorMessage = "Description length cannot exceed 1000 characters.")]
    public string? Description { get; set; }
    
    [Required]
    public int Year { get; set; }
    
    [MaxLength(255, ErrorMessage = "Director name length cannot exceed 255 characters.")]
    public string? Director { get; set; }
    
    public double? Rating { get; set; }
    
    public double? RuntimeMinutes { get; set; }
    
    [MaxLength(500, ErrorMessage = "Genre length cannot exceed 500 characters.")]
    public string? Genre { get; set; }
    
    [MaxLength(500, ErrorMessage = "PosterUrl length cannot exceed 500 characters.")]
    public string? PosterUrl { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.MinValue;
    
    public DateTime? UpdatedAt { get; set; } 
    
    // Foreign Model Relations
    public ICollection<Auditorium> Auditoriums { get; set; } = new List<Auditorium>();

}