using System.ComponentModel.DataAnnotations;

namespace MovieAPI.DTOs.Requests.Auditorium;

public class AddMovieToAuditoriumRequest
{
    [Required]
    public int MovieId { get; set; }
}
