using System.ComponentModel.DataAnnotations;

namespace MovieAPI.Models;

public class IdempotencyRecord
{
    [Key]
    public string IdempotencyKey { get; set; }
    [Required]
    public string RequestPath { get; set; }
    [Required]
    public int StatusCode { get; set; }
    [Required]
    public string ResponseBody { get; set; }
    [Required]
    public DateTime CreatedAt { get; set; }
    [Required]
    public DateTime ExpiresAt { get; set; }
}