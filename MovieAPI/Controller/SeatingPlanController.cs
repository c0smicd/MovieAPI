using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MovieAPI.Data;
using MovieAPI.DTOs.Response;

namespace MovieAPI.Controller;

[ApiController]
[Route("api/v1/seatingplan")]
public class SeatingPlanController : BaseController
{
    
    public SeatingPlanController(
        AppDbContext context,
        ILogger<SeatingPlanController> logger,
        IMemoryCache cache) : base(context, logger, cache)
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
            if (Cache.TryGetValue(CacheKeys.SeatingPlanById(id), out SeatingPlanDToResponse? cachedSeatingPlan))
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

            Cache.Set(CacheKeys.SeatingPlanById(id), seatingplan);
            Logger.LogInformation("Seating plan {id} retrieved", id);

            return Ok(seatingplan);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving seating plan {Id}.", id);
            return StatusCode(500, "Internal server error");
        }
    }

}