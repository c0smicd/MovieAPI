using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
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
        public static string SeatingPlanById(int id) => $"seating_plan_{id}";
        public const string PaginationKeys = "pagination_keys";
    }

    protected readonly AppDbContext Context;
    protected readonly ILogger Logger;
    protected readonly IDistributedCache Cache;

    protected BaseController(AppDbContext context, ILogger logger, IDistributedCache cache)
    {
        Context = context;
        Logger = logger;
        Cache = cache;
    }

    protected async Task<T?> GetCacheAsync<T>(string key)
    {
        var bytes = await Cache.GetAsync(key);
        if (bytes == null || bytes.Length == 0) return default;
        return JsonSerializer.Deserialize<T>(bytes);
    }

    protected async Task SetCacheAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var options = new DistributedCacheEntryOptions();
        if (expiry.HasValue)
            options.AbsoluteExpirationRelativeToNow = expiry;
        await Cache.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(value), options);
    }

    protected async Task RemoveCacheAsync(string key)
    {
        await Cache.RemoveAsync(key);
    }

    // Stores pagination cache keys in Redis so all replicas share the same invalidation list.
    protected async Task RegisterPaginationKeyAsync(string key)
    {
        var keys = await GetCacheAsync<List<string>>(CacheKeys.PaginationKeys) ?? [];
        if (!keys.Contains(key))
        {
            keys.Add(key);
            await SetCacheAsync(CacheKeys.PaginationKeys, keys, TimeSpan.FromHours(1));
        }
    }

    protected async Task<List<string>> GetPaginationKeysAsync()
    {
        return await GetCacheAsync<List<string>>(CacheKeys.PaginationKeys) ?? [];
    }

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
        const int timeToLifeFromNow = 5;
        var idempotencyRecord = new IdempotencyRecord
        {
            IdempotencyKey = idempotencyKey,
            RequestPath = requestPath,
            StatusCode = statusCode,
            ResponseBody = JsonSerializer.Serialize(responseBody),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(timeToLifeFromNow)
        };

        Context.IdempotencyRecords.Add(idempotencyRecord);
        await Context.SaveChangesAsync();
    }
}
