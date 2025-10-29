using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieAPI.Data;

namespace MovieAPI.Controller;

public abstract class BaseController : ControllerBase
{
    protected readonly AppDbContext Context;
    protected readonly ILogger Logger;

    protected BaseController(AppDbContext context, ILogger logger)
    {
        Context = context;
        Logger = logger;
    }

    protected async Task<ActionResult?> CheckIdempotencyAsync<T>(string? idempotencyKey)
    {
        if (string.IsNullOrEmpty(idempotencyKey))
            return null;

        var existingRecord = await Context.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey && r.ExpiresAt > DateTime.UtcNow);

        if (existingRecord == null)
            return null;


        Logger.LogWarning("Idempotent request with key '{key}' found, returning cached response", idempotencyKey);

        var cachedResponse = JsonSerializer.Deserialize<T>(existingRecord.ResponseBody);
        return StatusCode(existingRecord.StatusCode, cachedResponse);
    }
}