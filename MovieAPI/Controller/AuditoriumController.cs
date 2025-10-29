using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MovieAPI.Data;
using MovieAPI.DTOs.Requests.Auditorium;
using MovieAPI.DTOs.Response;
using MovieAPI.Models;

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

            var auditorium = await _context.Auditoriums
                .Where(a => a.Id == id)
                .Select(a => new AuditoriumDToResponse
                {
                    Id = a.Id,
                    AuditoriumName = a.AuditoriumName,
                    SeatingPlan = new SeatingPlanDToResponse
                    {
                        Id = a.SeatingPlan.Id,
                        LayoutJson = a.SeatingPlan.LayoutJson,
                        Description = a.SeatingPlan.Description
                    },
                    Movies = (a.Movies ?? Enumerable.Empty<Movie>())
                        .Select(m => new MovieDToResponse
                        {
                            Id = m.Id,
                            Title = m.Title,
                            Rating = m.Rating,
                            Genre = m.Genre,
                            PosterUrl = m.PosterUrl
                        }).ToList()
                })
                .FirstOrDefaultAsync();

            // Store in cache
            _cache.Set(id, auditorium, TimeSpan.FromMinutes(10));
            _logger.LogInformation("Auditorium {Id} retrieved from database and cached.", id);

            return Ok(auditorium);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving auditorium {Id}.", id);
            return StatusCode(500, "Internal server error");
        }
    }

    // ---------------------------------------------- CREATE ----------------------------------------------

    [HttpPost]
    [ProducesResponseType(201, Type = typeof(AuditoriumDToResponse))]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<AuditoriumDToResponse>> CreateAuditorium(
        [FromBody] AuditoriumDToCreate auditoriumDto,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogError("Invalid model state for creating auditorium.");
            return BadRequest(ModelState);
        }

        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existingRecord = await _context.IdempotencyRecords
                .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey && r.ExpiresAt > DateTime.UtcNow);

            if (existingRecord != null)
            {
                _logger.LogWarning("Idempotent request with key '{key}' found, returning cached response",
                    idempotencyKey); // Log Information

                var cachedResponse = JsonSerializer.Deserialize<MovieDToResponse>(existingRecord.ResponseBody);

                return StatusCode(existingRecord.StatusCode, cachedResponse);
            }
        }

        try
        {
            var seatingPlan = await _context.SeatingPlans
                .Where(s => s.Id == auditoriumDto.SeatingPlanId)
                .FirstOrDefaultAsync();

            if (seatingPlan == null)
            {
                _logger.LogWarning("Seating plan with ID {SeatingPlanId} not found.", auditoriumDto.SeatingPlanId);

                return BadRequest($"Seating plan with ID {auditoriumDto.SeatingPlanId} not found.");
            }

            var auditorium = new Auditorium
            {
                Id = auditoriumDto.Id,
                AuditoriumName = auditoriumDto.AuditoriumName,
                SeatingPlan = seatingPlan
            };

            _context.Auditoriums.Add(auditorium);
            await _context.SaveChangesAsync();

            var responseDto = new AuditoriumDToResponse
            {
                Id = auditorium.Id,
                AuditoriumName = auditorium.AuditoriumName,
                SeatingPlan = new SeatingPlanDToResponse
                {
                    Id = seatingPlan.Id,
                    LayoutJson = seatingPlan.LayoutJson,
                    Description = seatingPlan.Description
                },
                Movies = null
            };

            // Store idempotency record

            if (!string.IsNullOrEmpty(idempotencyKey))
            {
                var idempotencyRecord = new IdempotencyRecord
                {
                    IdempotencyKey = idempotencyKey,
                    RequestPath = HttpContext.Request.Path,
                    StatusCode = 201,
                    ResponseBody = JsonSerializer.Serialize(responseDto),
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5)
                };

                _context.IdempotencyRecords.Add(idempotencyRecord);

                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Created auditorium with ID {id}.", auditorium.Id);
            return CreatedAtAction(nameof(GetAuditoriumById), new { id = auditorium.Id }, responseDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating auditorium.");
            return StatusCode(500, "Internal server error");
        }
    }
}