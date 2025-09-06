using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;

namespace FlightBooking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RedisTestController : ControllerBase
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDistributedCache _cache;

    public RedisTestController(IConnectionMultiplexer redis, IDistributedCache cache)
    {
        _redis = redis;
        _cache = cache;
    }

    [HttpGet("connection")]
    public IActionResult TestConnection()
    {
        try
        {
            var database = _redis.GetDatabase();
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            
            var info = server.Info();
            var serverInfo = info.FirstOrDefault(x => x.Key == "Server");
            var redisVersion = serverInfo?.FirstOrDefault(x => x.Key == "redis_version").Value;

            return Ok(new { 
                Status = "Connected", 
                RedisVersion = redisVersion,
                IsConnected = _redis.IsConnected,
                EndPoints = _redis.GetEndPoints().Select(ep => ep.ToString()).ToArray(),
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { 
                Status = "Error", 
                Message = ex.Message,
                InnerException = ex.InnerException?.Message
            });
        }
    }

    [HttpPost("cache/set")]
    public async Task<IActionResult> SetCache([FromBody] CacheTestRequest request)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(request.ExpiryMinutes ?? 5)
            };

            await _cache.SetStringAsync(request.Key, request.Value, options);

            return Ok(new { 
                Status = "Success", 
                Message = $"Cached key '{request.Key}' with value '{request.Value}'",
                ExpiryMinutes = request.ExpiryMinutes ?? 5
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { 
                Status = "Error", 
                Message = ex.Message
            });
        }
    }

    [HttpGet("cache/get/{key}")]
    public async Task<IActionResult> GetCache(string key)
    {
        try
        {
            var value = await _cache.GetStringAsync(key);
            
            if (value == null)
            {
                return NotFound(new { 
                    Status = "Not Found", 
                    Message = $"Key '{key}' not found in cache"
                });
            }

            return Ok(new { 
                Status = "Found", 
                Key = key,
                Value = value,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { 
                Status = "Error", 
                Message = ex.Message
            });
        }
    }

    [HttpDelete("cache/delete/{key}")]
    public async Task<IActionResult> DeleteCache(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
            
            return Ok(new { 
                Status = "Success", 
                Message = $"Key '{key}' removed from cache"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { 
                Status = "Error", 
                Message = ex.Message
            });
        }
    }

    [HttpGet("info")]
    public IActionResult GetRedisInfo()
    {
        try
        {
            var database = _redis.GetDatabase();
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            
            var dbSize = server.DatabaseSize();
            var info = server.Info();
            
            var memoryInfo = info.FirstOrDefault(x => x.Key == "Memory");
            var usedMemory = memoryInfo?.FirstOrDefault(x => x.Key == "used_memory_human").Value;
            
            return Ok(new { 
                Status = "Success",
                DatabaseSize = dbSize,
                UsedMemory = usedMemory,
                IsConnected = _redis.IsConnected,
                Configuration = _redis.Configuration
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { 
                Status = "Error", 
                Message = ex.Message
            });
        }
    }
}

public class CacheTestRequest
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int? ExpiryMinutes { get; set; }
}
