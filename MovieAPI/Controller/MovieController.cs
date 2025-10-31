using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MovieAPI.Data;
using MovieAPI.DTOs.Requests.Movie;
using MovieAPI.DTOs.Response;
using MovieAPI.Models;

namespace MovieAPI.Controller;

[ApiController]
[Route("api/v1/films")]
public class MovieController : BaseController
{
    
    public MovieController(AppDbContext context, ILogger<MovieController> logger, IMemoryCache cache) : base(context,
        logger, cache)
    {
        
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
        if (Cache.TryGetValue(CacheKeys.MoviesByPage(page, pageSize), out MovieDToResponse[]? cachedFilms))
        {
            Logger.LogInformation("Returning cached movies for page {page}", page);
            return Ok(cachedFilms);
        }

        try
        {
            var films = await Context.Movies
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
                Logger.LogWarning("No films found for page '{page}'", page); // Log warning
                return NotFound(new { message = "No films found for the specified page" }); // Return 404
            }

            Cache.Set(CacheKeys.MoviesByPage(page, pageSize), films, TimeSpan.FromMinutes(10)); // Cache for 10 minutes
            RegisterPaginationKey(CacheKeys.MoviesByPage(page, pageSize));

            Logger.LogInformation("'{films.length}' films from page '{page}' where retrieved", films.Length,
                page); // Log information 
            return Ok(films); // Return 200
        }
        catch (Exception ex) // If server had a hiccup
        {
            Logger.LogError("Error while retrieving all movies: {ex}", ex); // Log Error

            return StatusCode(500, new { message = "Internal server error" }); // Return 500
        }
    }

    /// <summary>
    ///   Get all movies by auditorium id
    /// </summary>
    /// <param name="auditoriumId">ID of the auditorium</param>
    /// <returns></returns>
    [HttpGet("by-auditorium/{auditoriumId:int}")]
    [ProducesResponseType(200, Type = typeof(IEnumerable<MovieDToResponse>))]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<IEnumerable<MovieDToResponse>>> GetMoviesByAuditorium(int auditoriumId)
    {
        if (Cache.TryGetValue(CacheKeys.MoviesByAuditorium(auditoriumId), out MovieDToResponse[]? cachedFilms))
        {
            Logger.LogInformation("Returning cached movies for auditoriumId {auditoriumId}", auditoriumId);
            return Ok(cachedFilms);
        }

        try
        {
            var films = await Context.Movies
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
                Logger.LogWarning("No films found for auditoriumId '{auditoriumId}'", auditoriumId); // Log warning
                return NotFound(new { message = "No films found for the specified auditorium" }); // Return 404
            }

            Cache.Set(CacheKeys.MoviesByAuditorium(auditoriumId), films,
                TimeSpan.FromMinutes(5)); // Cache for 5 minutes

            Logger.LogInformation("'{films.length}' films for auditoriumId '{auditoriumId}' were retrieved",
                films.Length, auditoriumId); // Log information 
            return Ok(films); // Return 200
        }
        catch (Exception ex) // If server had a hiccup
        {
            Logger.LogError("Error while retrieving movies for auditoriumId '{auditoriumId}': {ex}", auditoriumId,
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
        if (Cache.TryGetValue(CacheKeys.MovieById(id), out MovieDToResponse? cachedMovie))
        {
            Logger.LogInformation("Returning cached movie for MovieId '{id}'", id); // Log Information
            return Ok(cachedMovie);
        }

        try
        {
            var movie = await Context.Movies
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
                Logger.LogWarning("MovieId '{id}' not found", id); // Log warning
                return NotFound(new { message = "Film not found" }); // Return 404
            }

            Cache.Set(CacheKeys.MovieById(id), movie, TimeSpan.FromMinutes(30)); // Cache for 30 minutes

            // Movie with id found
            Logger.LogInformation("MovieId '{id}' retrieved successfully", id); // Log Information
            return Ok(movie); // return 200
        }
        catch (Exception ex) // If server had a hiccup 
        {
            Logger.LogError("Error while retrieving movie '{id}: {ex}", id, ex); // Log Error
            return StatusCode(500, new { message = "Internal server error" }); // Return 500
        }
    }


    //---------------------------------------------- CREATE METHODS ------------------------------------------------

    [HttpPost]
    [ProducesResponseType(200, Type = typeof(MovieDToResponse))]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<MovieDToResponse>> CreateMovie(
        [FromBody] MovieDToCreate movieDto,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null)
    {
        if (!ModelState.IsValid)
        {
            Logger.LogError("Invalid model state for creating movie.");
            return BadRequest(ModelState);
        }

        var cached = await CheckIdempotencyAsync<MovieDToResponse>(idempotencyKey);

        if (cached != null)
            return cached;

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

            // Does there exist auditoriums to link?
            if (movieDto.AuditoriumIds is { Count: > 0 } auditoriumIds)
            {
                var existingCount = await Context.Auditoriums
                    .CountAsync(a => auditoriumIds.Contains(a.Id));

                if (existingCount != auditoriumIds.Count)
                {
                    Logger.LogWarning("One or more AuditoriumIds provided do not exist");
                    return NotFound(new { message = "One or more AuditoriumIds provided do not exist" });
                }

                // Link Auditoriums
                var auditoriums = await Context.Auditoriums
                    .Where(a => movieDto.AuditoriumIds.Contains(a.Id))
                    .ToListAsync();

                movie.Auditoriums = auditoriums;

                Context.Movies.Add(movie);
                await Context.SaveChangesAsync();
            }


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
                await CreateIdempotencyRecord(idempotencyKey, HttpContext.Request.Path, 201, responseDto);
            }
            

            // Clear every pagination cache entry as data has changed
            foreach (var paginationEntries in GetPaginationKeys())
                Cache.Remove(paginationEntries);

            Logger.LogInformation("MovieId '{id}' created successfully", movie.Id); // Log Information
            return CreatedAtAction(nameof(GetMovie), new { id = movie.Id }, responseDto); // Return 201
        }
        catch (Exception ex) // If server had a hiccup 
        {
            Logger.LogError("Error while creating movie: {ex}", ex); // Log Error
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
        if (!ModelState.IsValid)
        {
            Logger.LogError("Invalid model state for creating movie.");
            return BadRequest(ModelState);
        }

        try
        {
            var movie = await Context.Movies
                .Include(m => m.Auditoriums)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null)
            {
                Logger.LogWarning("MovieId '{id}' not found for update", id); // Log Warning
                return NotFound(new { message = "Film not found" }); // Return 404
            }

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

            // Check if the AuditoriumIds provided exist
            if (movieDto.AuditoriumIds is { Count: > 0 })
            {
                var existsEveryAuditorium = await Context.Auditoriums
                    .Select(a => a.Id)
                    .AllAsync(a => movieDto.AuditoriumIds.Contains(a));

                if(!existsEveryAuditorium)
                {
                    Logger.LogWarning("One or more AuditoriumIds provided do not exist");
                    return NotFound(new { message = "One or more AuditoriumIds provided do not exist" });
                }

                // Update Auditorium associations
                var auditoriums = await Context.Auditoriums
                    .Where(a => movieDto.AuditoriumIds.Contains(a.Id))
                    .ToListAsync();

                movie.Auditoriums = auditoriums;
            }

            await Context.SaveChangesAsync();

            Cache.Remove(CacheKeys.MovieById(id)); // Invalidate cache for this movie

            // Invalidate Cache for all auditoriums related to this movie
            var currentAuditoriumIds = movie.Auditoriums.Select(a => a.Id).ToList();

            foreach (var auditoriumId in currentAuditoriumIds)
                Cache.Remove(CacheKeys.MoviesByAuditorium(auditoriumId));

            Logger.LogInformation("MovieId '{id}' updated successfully", id); // Log Information
            return NoContent(); // Return 204
        }
        catch (Exception ex) // If server had a hiccup 
        {
            Logger.LogError("Error while updating movie '{id}': {ex}", id, ex); // Log Error
            return StatusCode(500, new { message = "Internal server error" }); // Return 500
        }
    }

    // ---------------------------------------------- DELETE METHODS ----------------------------------------------
    [HttpDelete("{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> DeleteMovie(int id)
    {
        try
        {
            var auditoriums = await Context.Movies
                .Where(m => m.Id == id)
                .SelectMany(m => m.Auditoriums)
                .Select(a => a.Id)
                .ToListAsync();

            var deletedRows = await Context.Movies
                .Where(m => m.Id == id)
                .ExecuteDeleteAsync();

            if (deletedRows == 0)
            {
                Logger.LogWarning("MovieId '{id}' not found", id);
                return NotFound(new { message = "Movie not found,  therefor nothing to delete" });
            }

            // Clear cache routine

            Cache.Remove(CacheKeys.MovieById(id));
            // Clear every pagination cache entry as data has changed
            // Yea I know, code duplication, but it's late and I don't want to refactor right now
            foreach (var paginationEntries in GetPaginationKeys())
                Cache.Remove(paginationEntries);


            // Invalidate Cache for all auditoriums related to this movie
            foreach (var auditoriumId in auditoriums)
                Cache.Remove(CacheKeys.AuditoriumById(auditoriumId));


            Logger.LogInformation("MovieId '{id}' successfully deleted", id);
            return Ok(new { message = "Movie deleted successfully" });
        }
        catch (Exception e)
        {
            Logger.LogError("Error while deleting movie '{id}': {ex}", id, e); // Log Error
            return StatusCode(500, new { message = "Internal server error" }); // Return 500
        }
    }
}