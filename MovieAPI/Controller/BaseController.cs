using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MovieAPI.Data;
using MovieAPI.Models;

namespace MovieAPI.Controller;

public abstract class BaseController : ControllerBase
{

    protected static class CacheKeys
    {
        public static string MovieById(int id) => $"movie_{id}";
        public static string MoviesByPage(int page, int pageSize) => $"movies_page_{page}_size_{pageSize}";
        public static string MoviesByAuditorium(int auditoriumId) => $"by_auditorium_{auditoriumId}";
        public static string AuditoriumById(int id) => $"auditorium_{id}";
    }


    protected readonly AppDbContext Context;
    protected readonly ILogger Logger;
    protected readonly IMemoryCache Cache;

    private readonly HashSet<string> _paginationKeys = new ();
    private readonly object _syncLock = new ();

    protected BaseController(AppDbContext context, ILogger logger, IMemoryCache cache)
    {
        Context = context;
        Logger = logger;
        Cache = cache;
    }
    
    /// <summary>
    ///  Checks for an existing idempotency record and returns the cached response if found.
    /// </summary>
    /// <param name="idempotencyKey"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    protected async Task<ActionResult?> CheckIdempotencyAsync<T>(string? idempotencyKey)
    {
        if (string.IsNullOrEmpty(idempotencyKey))
            return null;
        
        var requestPath = HttpContext.Request.Path.Value;
        
        var existingRecord = await Context.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey 
                                      && r.ExpiresAt > DateTime.UtcNow
                                      && r.RequestPath == requestPath);

        if (existingRecord == null)
            return null;


        Logger.LogWarning("Idempotent request with key '{key}' found, returning cached response", idempotencyKey);

        var cachedResponse = JsonSerializer.Deserialize<T>(existingRecord.ResponseBody);
        return StatusCode(existingRecord.StatusCode, cachedResponse);
    }
    
    protected async Task CreateIdempotencyRecord<T>(string idempotencyKey, string requestPath, int statusCode, T responseBody)
    {
        var idempotencyRecord = new IdempotencyRecord
        {
            IdempotencyKey = idempotencyKey,
            RequestPath = requestPath,
            StatusCode = statusCode,
            ResponseBody = JsonSerializer.Serialize(responseBody),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        Context.IdempotencyRecords.Add(idempotencyRecord);
        await Context.SaveChangesAsync();
    }

    //Question, should I handle caching here or in the calling controllers? NO
    protected void RegisterPaginationKey(string key)
    {
        lock(_syncLock) _paginationKeys.Add(key);

    }

    protected List<string> GetPaginationKeys()
    {
        lock(_syncLock) return _paginationKeys.ToList();
    }
}