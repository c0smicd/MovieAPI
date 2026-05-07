using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MovieAPI.Data;
using MovieAPI.DTOs.Requests.Movie;
using MovieAPI.DTOs.Response;
using MovieAPI.Models;

namespace MovieAPI.Controller;

[ApiController]
[Route("api/v1/films")]
public class MovieController : BaseController
{
    public MovieController(AppDbContext context, ILogger<MovieController> logger, IDistributedCache cache)
        : base(context, logger, cache)
    {
    }

    // ---------------------------------------------- GET METHODS ----------------------------------------------

    [HttpGet]
    [ProducesResponseType(200, Type = typeof(IEnumerable<MovieDToResponse>))]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<IEnumerable<MovieDToResponse>>> GetMovies(int page = 1, int pageSize = 10)
    {
        var cachedFilms = await GetCacheAsync<MovieDToResponse[]>(CacheKeys.MoviesByPage(page, pageSize));
        if (cachedFilms != null)
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
                Logger.LogWarning("No films found for page '{page}'", page);
                return NotFound(new { message = "No films found for the specified page" });
            }

            await SetCacheAsync(CacheKeys.MoviesByPage(page, pageSize), films, TimeSpan.FromMinutes(10));
            await RegisterPaginationKeyAsync(CacheKeys.MoviesByPage(page, pageSize));

            Logger.LogInformation("'{films.length}' films from page '{page}' where retrieved", films.Length, page);
            return Ok(films);
        }
        catch (Exception ex)
        {
            Logger.LogError("Error while retrieving all movies: {ex}", ex);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("by-auditorium/{auditoriumId:int}")]
    [ProducesResponseType(200, Type = typeof(IEnumerable<MovieDToResponse>))]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<IEnumerable<MovieDToResponse>>> GetMoviesByAuditorium(int auditoriumId)
    {
        var cachedFilms = await GetCacheAsync<MovieDToResponse[]>(CacheKeys.MoviesByAuditorium(auditoriumId));
        if (cachedFilms != null)
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
                Logger.LogWarning("No films found for auditoriumId '{auditoriumId}'", auditoriumId);
                return NotFound(new { message = "No films found for the specified auditorium" });
            }

            await SetCacheAsync(CacheKeys.MoviesByAuditorium(auditoriumId), films, TimeSpan.FromMinutes(5));

            Logger.LogInformation("'{films.length}' films for auditoriumId '{auditoriumId}' were retrieved",
                films.Length, auditoriumId);
            return Ok(films);
        }
        catch (Exception ex)
        {
            Logger.LogError("Error while retrieving movies for auditoriumId '{auditoriumId}': {ex}", auditoriumId, ex);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(200, Type = typeof(MovieDToResponse))]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<MovieDToResponse>> GetMovie(int id)
    {
        var cachedMovie = await GetCacheAsync<MovieDToResponse>(CacheKeys.MovieById(id));
        if (cachedMovie != null)
        {
            Logger.LogInformation("Returning cached movie for MovieId '{id}'", id);
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

            if (movie == null)
            {
                Logger.LogWarning("MovieId '{id}' not found", id);
                return NotFound(new { message = "Film not found" });
            }

            await SetCacheAsync(CacheKeys.MovieById(id), movie, TimeSpan.FromMinutes(30));

            Logger.LogInformation("MovieId '{id}' retrieved successfully", id);
            return Ok(movie);
        }
        catch (Exception ex)
        {
            Logger.LogError("Error while retrieving movie '{id}: {ex}", id, ex);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // ---------------------------------------------- CREATE METHODS ------------------------------------------------

    [HttpPost]
    [ProducesResponseType(201, Type = typeof(MovieDToResponse))]
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

        var idempotentResponse = await CheckIdempotencyAsync<MovieDToResponse>(idempotencyKey);
        if (idempotentResponse != null)
            return idempotentResponse;

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

            Context.Movies.Add(movie);
            await Context.SaveChangesAsync();

            var responseDto = new MovieDToResponse
            {
                Id = movie.Id,
                Title = movie.Title,
                Rating = movie.Rating,
                Genre = movie.Genre,
                PosterUrl = movie.PosterUrl
            };

            if (!string.IsNullOrEmpty(idempotencyKey))
                await CreateIdempotencyRecord(idempotencyKey, HttpContext.Request.Path, 201, responseDto);

            foreach (var paginationKey in await GetPaginationKeysAsync())
                await RemoveCacheAsync(paginationKey);

            await RemoveCacheAsync(CacheKeys.MovieById(movie.Id));

            Logger.LogInformation("MovieId '{id}' created successfully", movie.Id);
            return CreatedAtAction(nameof(GetMovie), new { id = movie.Id }, responseDto);
        }
        catch (Exception ex)
        {
            Logger.LogError("Error while creating movie: {ex}", ex);
            return StatusCode(500, new { message = "Internal server error" });
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
                Logger.LogWarning("MovieId '{id}' not found for update", id);
                return NotFound(new { message = "Film not found" });
            }

            movie.Title = movieDto.Title ?? movie.Title;
            movie.Description = movieDto.Description ?? movie.Description;
            movie.Year = movieDto.Year != 0 ? movieDto.Year : movie.Year;
            movie.Director = movieDto.Director ?? movie.Director;
            movie.Rating = movieDto.Rating != 0 ? movieDto.Rating : movie.Rating;
            movie.RuntimeMinutes = movieDto.RuntimeMinutes != 0 ? movieDto.RuntimeMinutes : movie.RuntimeMinutes;
            movie.Genre = movieDto.Genre ?? movie.Genre;
            movie.PosterUrl = movieDto.PosterUrl ?? movie.PosterUrl;
            movie.UpdatedAt = DateTime.UtcNow;

            await Context.SaveChangesAsync();

            await RemoveCacheAsync(CacheKeys.MovieById(id));

            var currentAuditoriumIds = movie.Auditoriums.Select(a => a.Id).ToList();
            foreach (var auditoriumId in currentAuditoriumIds)
                await RemoveCacheAsync(CacheKeys.MoviesByAuditorium(auditoriumId));

            Logger.LogInformation("MovieId '{id}' updated successfully", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            Logger.LogError("Error while updating movie '{id}': {ex}", id, ex);
            return StatusCode(500, new { message = "Internal server error" });
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

            await RemoveCacheAsync(CacheKeys.MovieById(id));

            foreach (var paginationKey in await GetPaginationKeysAsync())
                await RemoveCacheAsync(paginationKey);

            foreach (var auditoriumId in auditoriums)
                await RemoveCacheAsync(CacheKeys.AuditoriumById(auditoriumId));

            Logger.LogInformation("MovieId '{id}' successfully deleted", id);
            return Ok(new { message = "Movie deleted successfully" });
        }
        catch (Exception e)
        {
            Logger.LogError("Error while deleting movie '{id}': {ex}", id, e);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
