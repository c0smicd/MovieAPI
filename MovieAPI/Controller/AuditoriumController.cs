using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MovieAPI.Data;
using MovieAPI.DTOs.Response;

namespace MovieAPI.Controller;

[ApiController]
[Route("api/v1/auditoriums")]
public class AuditoriumController : ControllerBase
{
    private readonly IMemoryCache _cache;
    private readonly AppDbContext _context;
    private readonly ILogger<MovieController> _logger;

    public AuditoriumController(
        AppDbContext context,
        ILogger<MovieController> logger,
        IMemoryCache cache)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
    }

    // ---------------------------------------------- GET METHODS ----------------------------------------------
    [HttpGet("{id:int}")]
    [ProducesResponseType(200, Type = typeof(AuditoriumDToResponse))]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<AuditoriumDToResponse>> GetAuditoriumById(int id)
    {
        try
        {
            // Check cache first
            if (_cache.TryGetValue(id, out AuditoriumDToResponse? cachedAuditorium))
            {
                _logger.LogInformation("Auditorium {Id} retrieved from cache.", id);
                return Ok(cachedAuditorium);
            }

            var auditorium = await _context.Auditoriums.FindAsync(id);
            if (auditorium == null)
            {
                _logger.LogWarning("Auditorium {Id} not found.", id);
                return NotFound();
            }

            var auditoriumDto = new AuditoriumDToResponse
            {
                // Map properties from auditorium to auditoriumDto
            };

            // Store in cache
            _cache.Set(id, auditoriumDto, TimeSpan.FromMinutes(10));
            _logger.LogInformation("Auditorium {Id} retrieved from database and cached.", id);

            return Ok(auditoriumDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving auditorium {Id}.", id);
            return StatusCode(500, "Internal server error");
        }
    }
}