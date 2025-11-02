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
public class AuditoriumController : BaseController
{

    public AuditoriumController(
        AppDbContext context,
        ILogger<AuditoriumController> logger,
        IMemoryCache cache) : base(context, logger, cache)
    {
        
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
            if (Cache.TryGetValue(id, out AuditoriumDToResponse? cachedAuditorium))
            {
                Logger.LogInformation("Auditorium {Id} retrieved from cache.", id);
                return Ok(cachedAuditorium);
            }
            // Get the auditorium with SeatingPlan and if there are movies associated with it
            var auditorium = await Context.Auditoriums
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
                    Movies = a.Movies
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
            Cache.Set(CacheKeys.AuditoriumById(id), auditorium, TimeSpan.FromMinutes(10));
            Logger.LogInformation("Auditorium {Id} retrieved from database and cached.", id);

            return Ok(auditorium);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving auditorium {Id}.", id);
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
            Logger.LogError("Invalid model state for creating auditorium.");
            return BadRequest(ModelState);
        }

        // Check for idempotency
        var idempotentResponse = await CheckIdempotencyAsync<AuditoriumDToResponse>(idempotencyKey);
        if (idempotentResponse != null)
            return BadRequest(idempotentResponse);

        // Idempotency key not found, proceed with creation
        try
        {
            // Does the seating plan exist?
            var seatingPlan = await Context.SeatingPlans
                .Where(s => s.Id == auditoriumDto.SeatingPlanId)
                .FirstOrDefaultAsync();

            if (seatingPlan == null)
            {
                Logger.LogWarning("Seating plan with ID {SeatingPlanId} not found.", auditoriumDto.SeatingPlanId);

                return BadRequest($"Seating plan with ID {auditoriumDto.SeatingPlanId} not found.");
            }
            // If so create the auditorium accordingly
            var auditorium = new Auditorium
            {
                Id = auditoriumDto.Id,
                AuditoriumName = auditoriumDto.AuditoriumName,
                SeatingPlan = seatingPlan
            };

            Context.Auditoriums.Add(auditorium);
            await Context.SaveChangesAsync();
            
            // Prepare response DTO
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
                await CreateIdempotencyRecord(idempotencyKey, HttpContext.Request.Path, 201, responseDto);
            }

            Logger.LogInformation("Created auditorium with ID {id}.", auditorium.Id);
            return CreatedAtAction(nameof(GetAuditoriumById), new { id = auditorium.Id }, responseDto);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating auditorium.");
            return StatusCode(500, "Internal server error");
        }
    }

    // ---------------------------------------------- UPDATE/PATCH ----------------------------------------------

    [HttpPatch("{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> UpdateAuditorium(int id, [FromBody] AuditoriumDToPatch auditoriumDto)
    {
        if (!ModelState.IsValid)
        {
            Logger.LogError("Invalid model state for updating auditorium {Id}.", id);
            return BadRequest(ModelState);
        }

        try
        {
            var auditorium = await Context.Auditoriums.FindAsync(id);
            if (auditorium == null)
            {
                Logger.LogWarning("Auditorium with ID {Id} not found for update.", id);
                return NotFound();
            }

            auditorium.AuditoriumName = auditoriumDto.AuditoriumName ?? auditorium.AuditoriumName;

            await Context.SaveChangesAsync();

            Logger.LogInformation("Updated auditorium with ID {Id}.", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating auditorium {Id}.", id);
            return StatusCode(500, "Internal server error");
        }
    }

    // ---------------------------------------------- MOVIE MANAGEMENT ----------------------------------------------

    [HttpPost("{id:int}/movies")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> AddMovieToAuditorium(int id, [FromBody] AddMovieToAuditoriumRequest request)
    {
        if (!ModelState.IsValid)
        {
            Logger.LogError("Invalid model state for adding movie to auditorium {Id}.", id);
            return BadRequest(ModelState);
        }

        try
        {
            var auditorium = await Context.Auditoriums
                .Include(a => a.Movies)
                .FirstOrDefaultAsync(a => a.Id == id);
            
            if (auditorium == null)
            {
                Logger.LogWarning("Auditorium with ID {Id} not found.", id);
                return NotFound($"Auditorium with ID {id} not found.");
            }

            var movie = await Context.Movies.FindAsync(request.MovieId);
            if (movie == null)
            {
                Logger.LogWarning("Movie with ID {MovieId} not found.", request.MovieId);
                return BadRequest($"Movie with ID {request.MovieId} not found.");
            }

            if (auditorium.Movies.Any(m => m.Id == request.MovieId))
            {
                Logger.LogWarning("Movie {MovieId} is already associated with auditorium {Id}.", request.MovieId, id);
                return BadRequest($"Movie with ID {request.MovieId} is already associated with this auditorium.");
            }

            auditorium.Movies.Add(movie);
            await Context.SaveChangesAsync();

            Logger.LogInformation("Added movie {MovieId} to auditorium {Id}.", request.MovieId, id);
            return NoContent();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error adding movie to auditorium {Id}.", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id:int}/movies/{movieId:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> RemoveMovieFromAuditorium(int id, int movieId)
    {
        try
        {
            var auditorium = await Context.Auditoriums
                .Include(a => a.Movies)
                .FirstOrDefaultAsync(a => a.Id == id);
            
            if (auditorium == null)
            {
                Logger.LogWarning("Auditorium with ID {Id} not found.", id);
                return NotFound($"Auditorium with ID {id} not found.");
            }

            var movie = auditorium.Movies.FirstOrDefault(m => m.Id == movieId);
            if (movie == null)
            {
                Logger.LogWarning("Movie {MovieId} is not associated with auditorium {Id}.", movieId, id);
                return NotFound($"Movie with ID {movieId} is not associated with this auditorium.");
            }

            auditorium.Movies.Remove(movie);
            await Context.SaveChangesAsync();

            Logger.LogInformation("Removed movie {MovieId} from auditorium {Id}.", movieId, id);
            return NoContent();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error removing movie {MovieId} from auditorium {Id}.", movieId, id);
            return StatusCode(500, "Internal server error");
        }
    }

    // ---------------------------------------------- SEATING PLAN MANAGEMENT ----------------------------------------------

    [HttpPatch("{id:int}/seatingplan")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> UpdateAuditoriumSeatingPlan(int id, [FromBody] UpdateSeatingPlanRequest request)
    {
        if (!ModelState.IsValid)
        {
            Logger.LogError("Invalid model state for updating seating plan for auditorium {Id}.", id);
            return BadRequest(ModelState);
        }

        try
        {
            var auditorium = await Context.Auditoriums.FindAsync(id);
            if (auditorium == null)
            {
                Logger.LogWarning("Auditorium with ID {Id} not found.", id);
                return NotFound($"Auditorium with ID {id} not found.");
            }

            var seatingPlan = await Context.SeatingPlans.FindAsync(request.SeatingPlanId);

            if (seatingPlan == null)
            {
                Logger.LogWarning("Seating plan with ID {SeatingPlanId} not found.", request.SeatingPlanId);
                return BadRequest($"Seating plan with ID {request.SeatingPlanId} not found.");
            }

            auditorium.SeatingPlan = seatingPlan;

            await Context.SaveChangesAsync();

            Logger.LogInformation("Updated seating plan for auditorium {Id} to {SeatingPlanId}.", id, request.SeatingPlanId);
            return NoContent();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating seating plan for auditorium {Id}.", id);
            return StatusCode(500, "Internal server error");
        }
    }
}