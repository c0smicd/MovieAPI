using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MovieAPI.Constants;
using MovieAPI.Data;
using MovieAPI.DTOs.Requests.Movie;
using MovieAPI.DTOs.Response;
using MovieAPI.Models;

namespace MovieAPI.Controller;

[ApiController]
[Route("api/v1/films")]
public class MovieController : ControllerBase
{
    private readonly IMemoryCache _cache;
    private readonly AppDbContext _context;
    private readonly ILogger<MovieController> _logger;


    public MovieController(AppDbContext context, ILogger<MovieController> logger, IMemoryCache cache)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
    }

    // ---------------------------------------------- GET METHODS ----------------------------------------------


    /// <summary>
    ///     Get all movies with pagination
    /// </summary>
    /// <param name="page">The current page</param>
    /// <param name="pageSize">The size of every page</param>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(200, Type = typeof(IEnumerable<MovieDToResponse>))]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<IEnumerable<MovieDToResponse>>> GetMovies(int page = 1, int pageSize = 10)
    {
        if (_cache.TryGetValue(CacheKeys.MoviesByPage(page, pageSize), out MovieDToResponse[]? cachedFilms))
        {
            _logger.LogInformation("Returning cached movies for page {page}", page);
            return Ok(cachedFilms);
        }

        try
        {
            var films = await _context.Movies
                .OrderBy(m => m.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MovieDToResponse
                {
                    Id = m.Id,
                    Title = m.Title,
                    Rating = m.Rating,
                    Genre = m.Genre,
                    PosterUrl = m.PosterUrl
                })
                .ToArrayAsync();

            if (films.Length == 0)
            {
                _logger.LogWarning("No films found for page '{page}'", page); // Log warning
                return NotFound(new { message = "No films found for the specified page" }); // Return 404
            }

            _cache.Set(CacheKeys.MoviesByPage(page, pageSize), films, TimeSpan.FromMinutes(10)); // Cache for 10 minutes

            _logger.LogInformation("'{films.length}' films from page '{page}' where retrieved", films.Length,
                page); // Log information 
            return Ok(films); // Return 200
        }
        catch (Exception ex) // If server had a hiccup
        {
            _logger.LogError("Error while retrieving all movies: {ex}", ex); // Log Error

            return StatusCode(500, new { message = "Internal server error" }); // Return 500
        }
    }

    /// <summary>
    ///   Get all movies by auditorium id
    /// </summary>
    /// <param name="auditoriumId">Id of the auditorium</param>
    /// <returns></returns>
    [HttpGet("by-auditorium/{auditoriumId:int}")]
    [ProducesResponseType(200, Type = typeof(IEnumerable<MovieDToResponse>))]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<IEnumerable<MovieDToResponse>>> GetMovies(int auditoriumId)
    {
        if (_cache.TryGetValue(CacheKeys.MoviesByAuditorium(auditoriumId), out MovieDToResponse[]? cachedFilms))
        {
            _logger.LogInformation("Returning cached movies for auditoriumId {auditoriumId}", auditoriumId);
            return Ok(cachedFilms);
        }

        try
        {
            var films = await _context.Movies
                .Where(m => m.Auditoriums.Any(a => a.Id == auditoriumId))
                .Select(m => new MovieDToResponse
                {
                    Id = m.Id,
                    Title = m.Title,
                    Rating = m.Rating,
                    Genre = m.Genre,
                    PosterUrl = m.PosterUrl
                })
                .ToArrayAsync();

            if (films.Length == 0)
            {
                _logger.LogWarning("No films found for auditoriumId '{auditoriumId}'", auditoriumId); // Log warning
                return NotFound(new { message = "No films found for the specified auditorium" }); // Return 404
            }

            _cache.Set(CacheKeys.MoviesByAuditorium(auditoriumId), films,
                TimeSpan.FromMinutes(5)); // Cache for 5 minutes

            _logger.LogInformation("'{films.length}' films for auditoriumId '{auditoriumId}' were retrieved",
                films.Length, auditoriumId); // Log information 
            return Ok(films); // Return 200
        }
        catch (Exception ex) // If server had a hiccup
        {
            _logger.LogError("Error while retrieving movies for auditoriumId '{auditoriumId}': {ex}", auditoriumId,
                ex); // Log Error

            return StatusCode(500, new { message = "Internal server error" }); // Return 500
        }
    }

    /// <summary>
    ///     Get movie by id
    /// </summary>
    /// <param name="id"> The ID of the movie </param>
    /// <returns>
    ///     It returns the movie with the specified ID.
    /// </returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(200, Type = typeof(MovieDToResponse))]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<MovieDToResponse>> GetMovie(int id)
    {
        if (_cache.TryGetValue(CacheKeys.MovieById(id), out MovieDToResponse? cachedMovie))
        {
            _logger.LogInformation("Returning cached movie for MovieId '{id}'", id); // Log Information
            return Ok(cachedMovie);
        }

        try
        {
            var movie = await _context.Movies
                .Where(m => m.Id == id)
                .Select(m => new MovieDToResponse
                {
                    Id = m.Id,
                    Title = m.Title,
                    Rating = m.Rating,
                    Genre = m.Genre,
                    PosterUrl = m.PosterUrl
                })
                .FirstOrDefaultAsync();

            if (movie == null) // If movie is not found
            {
                _logger.LogWarning("MovieId '{id}' not found", id); // Log warning 
                return NotFound(new { message = "Film not found" }); // Return 404
            }

            _cache.Set(CacheKeys.MovieById(id), movie, TimeSpan.FromMinutes(30)); // Cache for 30 minutes

            // Movie with id found
            _logger.LogInformation("MovieId '{id}' retrieved successfully", id); // Log Information
            return Ok(movie); // return 200
        }
        catch (Exception ex) // If server had a hiccup 
        {
            _logger.LogError("Error while retrieving movie '{id}: {ex}", id, ex); // Log Error
            return StatusCode(500, new { message = "Internal server error" }); // Return 500
        }
    }


    //---------------------------------------------- CREATE METHODS ------------------------------------------------

    [HttpPost]
    public async Task<ActionResult<MovieDToResponse>> CreateMovie(
        [FromBody] MovieDToCreate movieDto,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existingRecord = await _context.IdempotencyRecords
                .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey && r.ExpiresAt > DateTime.UtcNow);

            if (existingRecord != null)
            {
                _logger.LogWarning("{time} - Idempotent request with key '{key}' found, returning cached response",
                    DateTime.UtcNow, idempotencyKey); // Log Information

                var cachedResponse = JsonSerializer.Deserialize<MovieDToResponse>(existingRecord.ResponseBody);

                return StatusCode(existingRecord.StatusCode, cachedResponse);
            }
        }

        try
        {
            var movie = new Movie
            {
                Title = movieDto.Title,
                Description = movieDto.Description,
                Year = movieDto.Year,
                Director = movieDto.Director,
                Rating = movieDto.Rating,
                RuntimeMinutes = movieDto.RuntimeMinutes,
                Genre = movieDto.Genre,
                PosterUrl = movieDto.PosterUrl,
                CreatedAt = movieDto.CreatedAt,
                UpdatedAt = movieDto.UpdatedAt
            };

            // Link Auditoriums
            if (movieDto.AuditoriumIds is { Count: > 0 } auditoriumIds)
            {
                var auditoriums = await _context.Auditoriums
                    .Where(a => auditoriumIds.Contains(a.Id))
                    .ToListAsync();

                movie.Auditoriums = auditoriums;
            }

            _context.Movies.Add(movie);
            await _context.SaveChangesAsync();

            var responseDto = new MovieDToResponse
            {
                Id = movie.Id,
                Title = movie.Title,
                Rating = movie.Rating,
                Genre = movie.Genre,
                PosterUrl = movie.PosterUrl
            };

            // Store Idempotency Record

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

            // Clear every cache related to movies upon creation, because cannot distinguish what data is stale, caused by Pagination
            if (_cache is MemoryCache concreteMemoryCache) concreteMemoryCache.Clear();

            _logger.LogInformation("MovieId '{id}' created successfully", movie.Id); // Log Information
            return CreatedAtAction(nameof(GetMovie), new { id = movie.Id }, responseDto); // Return 201
        }
        catch (Exception ex) // If server had a hiccup 
        {
            _logger.LogError("Error while creating movie: {ex}", ex); // Log Error
            return StatusCode(500, new { message = "Internal server error" }); // Return 500
        }
    }


    // ---------------------------------------------- UPDATE/PATCH ----------------------------------------------


    [HttpPatch("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> UpdateMovie(int id, [FromBody] MovieDToPatch movieDto)
    {
        try
        {
            var movie = await _context.Movies
                .Include(m => m.Auditoriums)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null)
            {
                _logger.LogWarning("MovieId '{id}' not found for update", id); // Log Warning
                return NotFound(new { message = "Film not found" }); // Return 404
            }

            var currentAuditoriumIds = movie.Auditoriums.Select(a => a.Id).ToList();

            // Update fields if provided
            movie.Title = movieDto.Title ?? movie.Title;
            movie.Description = movieDto.Description ?? movie.Description;
            movie.Year = movieDto.Year != 0 ? movieDto.Year : movie.Year;
            movie.Director = movieDto.Director ?? movie.Director;
            movie.Rating = movieDto.Rating != 0 ? movieDto.Rating : movie.Rating;
            movie.RuntimeMinutes = movieDto.RuntimeMinutes != 0 ? movieDto.RuntimeMinutes : movie.RuntimeMinutes;
            movie.Genre = movieDto.Genre ?? movie.Genre;
            movie.PosterUrl = movieDto.PosterUrl ?? movie.PosterUrl;
            movie.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _cache.Remove(CacheKeys.MovieById(id)); // Invalidate cache for this movie

            // Invalidate Cache for all auditoriums related to this movie
            foreach (var auditoriumId in currentAuditoriumIds)
                _cache.Remove(CacheKeys.MoviesByAuditorium(auditoriumId));

            _logger.LogInformation("MovieId '{id}' updated successfully", id); // Log Information
            return NoContent(); // Return 204
        }
        catch (Exception ex) // If server had a hiccup 
        {
            _logger.LogError("Error while updating movie '{id}': {ex}", id, ex); // Log Error
            return StatusCode(500, new { message = "Internal server error" }); // Return 500
        }
    }
}