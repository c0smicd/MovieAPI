using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MovieAPI.Data;
using MovieAPI.DTOs.Requests.SeatingPlan;
using MovieAPI.DTOs.Response;
using MovieAPI.Models;

namespace MovieAPI.Controller;

[ApiController]
[Route("api/v1/seatingplan")]
public class SeatingPlanController : BaseController
{
    public SeatingPlanController(
        AppDbContext context,
        ILogger<SeatingPlanController> logger,
        IDistributedCache cache) : base(context, logger, cache)
    {
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(200, Type = typeof(SeatingPlanDToResponse))]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<SeatingPlanDToResponse>> GetSeatingPlanById(int id)
    {
        try
        {
            var cachedSeatingPlan = await GetCacheAsync<SeatingPlanDToResponse>(CacheKeys.SeatingPlanById(id));
            if (cachedSeatingPlan != null)
            {
                Logger.LogInformation("Seating plan {id} retrieved from cache", id);
                return Ok(cachedSeatingPlan);
            }

            var seatingplan = await Context.SeatingPlans
                .Where(s => s.Id == id)
                .Select(s => new SeatingPlanDToResponse
                {
                    Id = s.Id,
                    PlanName = s.PlanName,
                    LayoutJson = s.LayoutJson,
                    Description = s.Description
                }).FirstOrDefaultAsync();

            await SetCacheAsync(CacheKeys.SeatingPlanById(id), seatingplan);
            Logger.LogInformation("Seating plan {id} retrieved", id);

            return Ok(seatingplan);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving seating plan {Id}.", id);
            return StatusCode(500, "Internal server error");
        }
    }

    // ---------------------------------------------- CREATE ----------------------------------------------

    [HttpPost]
    [ProducesResponseType(201, Type = typeof(SeatingPlanDToResponse))]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<SeatingPlanDToResponse>> CreateSeatingPlan(
        [FromBody] SeatingPlanDToCreate seatingPlanDTo,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null)
    {
        if (!ModelState.IsValid)
        {
            Logger.LogError("Invalid model state for creating seating plan");
            return BadRequest(ModelState);
        }

        var idempotencyResponse = await CheckIdempotencyAsync<SeatingPlanDToResponse>(idempotencyKey);
        if (idempotencyResponse != null)
            return idempotencyResponse;

        try
        {
            var seatplan = new SeatingPlan
            {
                PlanName = seatingPlanDTo.PlanName,
                Description = seatingPlanDTo.Description,
                LayoutJson = seatingPlanDTo.LayoutJson,
            };

            var responseDto = new SeatingPlanDToResponse
            {
                Id = seatplan.Id,
                PlanName = seatplan.PlanName,
                Description = seatplan.Description,
                LayoutJson = seatplan.LayoutJson
            };

            if (!string.IsNullOrEmpty(idempotencyKey))
                await CreateIdempotencyRecord(idempotencyKey, HttpContext.Request.Path, 201, responseDto);

            Logger.LogInformation("Seating plan {id} created", seatplan.Id);
            return CreatedAtAction(nameof(GetSeatingPlanById), new { seatplan.Id }, seatplan);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating seating plan");
            return StatusCode(500, "Internal server error");
        }
    }
}
