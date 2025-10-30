using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MovieAPI.Data;

namespace MovieAPI.Controller;

[ApiController]
[Microsoft.AspNetCore.Components.Route("api/v1/seatingplans")]
public class SeatingPlanController : BaseController
{
    
    public SeatingPlanController(
        AppDbContext context,
        ILogger<MovieController> logger,
        IMemoryCache cache) : base(context, logger, cache)
    {
        
    }


}