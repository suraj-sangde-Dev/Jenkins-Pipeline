using StackExchange.Redis;
using System.Text.Json;
using TeleICU.API.DTOs.CallDtos;

namespace TeleICU.API.Services;

/// <summary>
/// Service for managing call encounters in Redis with automatic 30-minute expiration
/// </summary>
public class EncounterStorageService
{
    private readonly IDatabase _db;
    private readonly ILogger<EncounterStorageService> _logger;
    private const int RECONNECTION_WINDOW_MINUTES = 30;
    private const string ENCOUNTER_KEY_PREFIX = "encounter:";
    private const string USER_CONNECTION_KEY_PREFIX = "userconn:";

    public EncounterStorageService(IConnectionMultiplexer redis, ILogger<EncounterStorageService> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    /// <summary>
    /// Store encounter data with 30-minute TTL
    /// </summary>
    public async Task<bool> SaveEncounterAsync(string encounterId, EncounterData encounterData)
    {
        try
        {
            var key = $"{ENCOUNTER_KEY_PREFIX}{encounterId}";
            var json = JsonSerializer.Serialize(encounterData);
            var expiry = TimeSpan.FromMinutes(RECONNECTION_WINDOW_MINUTES);
            
            var result = await _db.StringSetAsync(key, json, expiry);
            
            if (result)
            {
                _logger.LogInformation("Encounter {EncounterId} saved to Redis with {Minutes} minute TTL", 
                    encounterId, RECONNECTION_WINDOW_MINUTES);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving encounter {EncounterId} to Redis", encounterId);
            return false;
        }
    }

    /// <summary>
    /// Get encounter data from Redis
    /// </summary>
    public async Task<EncounterData?> GetEncounterAsync(string encounterId)
    {
        try
        {
            var key = $"{ENCOUNTER_KEY_PREFIX}{encounterId}";
            var json = await _db.StringGetAsync(key);
            
            if (json.IsNullOrEmpty)
            {
                _logger.LogDebug("Encounter {EncounterId} not found in Redis", encounterId);
                return null;
            }
            
            return JsonSerializer.Deserialize<EncounterData>(json!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving encounter {EncounterId} from Redis", encounterId);
            return null;
        }
    }

    /// <summary>
    /// Update encounter data and refresh TTL
    /// </summary>
    public async Task<bool> UpdateEncounterAsync(string encounterId, EncounterData encounterData)
    {
        try
        {
            var key = $"{ENCOUNTER_KEY_PREFIX}{encounterId}";
            var json = JsonSerializer.Serialize(encounterData);
            
            // Check remaining TTL
            var ttl = await _db.KeyTimeToLiveAsync(key);
            
            if (!ttl.HasValue || ttl.Value.TotalMinutes <= 0)
            {
                _logger.LogWarning("Encounter {EncounterId} has expired or doesn't exist", encounterId);
                return false;
            }
            
            // Update with remaining TTL (don't reset the 30-minute timer)
            var result = await _db.StringSetAsync(key, json, ttl.Value);
            
            if (result)
            {
                _logger.LogDebug("Encounter {EncounterId} updated in Redis", encounterId);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating encounter {EncounterId} in Redis", encounterId);
            return false;
        }
    }

    /// <summary>
    /// Delete encounter from Redis
    /// </summary>
    public async Task<bool> DeleteEncounterAsync(string encounterId)
    {
        try
        {
            var key = $"{ENCOUNTER_KEY_PREFIX}{encounterId}";
            var result = await _db.KeyDeleteAsync(key);
            
            if (result)
            {
                _logger.LogInformation("Encounter {EncounterId} deleted from Redis", encounterId);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting encounter {EncounterId} from Redis", encounterId);
            return false;
        }
    }

    /// <summary>
    /// Get remaining time for encounter in minutes
    /// </summary>
    public async Task<int> GetRemainingMinutesAsync(string encounterId)
    {
        try
        {
            var key = $"{ENCOUNTER_KEY_PREFIX}{encounterId}";
            var ttl = await _db.KeyTimeToLiveAsync(key);
            
            if (!ttl.HasValue || ttl.Value.TotalMinutes <= 0)
            {
                return 0;
            }
            
            return (int)Math.Ceiling(ttl.Value.TotalMinutes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting TTL for encounter {EncounterId}", encounterId);
            return 0;
        }
    }

    /// <summary>
    /// Check if encounter exists and is still valid
    /// </summary>
    public async Task<bool> IsEncounterValidAsync(string encounterId)
    {
        try
        {
            var key = $"{ENCOUNTER_KEY_PREFIX}{encounterId}";
            return await _db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking encounter {EncounterId} validity", encounterId);
            return false;
        }
    }

    /// <summary>
    /// Store user connection mapping (userId -> connectionId)
    /// </summary>
    public async Task<bool> SaveUserConnectionAsync(string userId, string connectionId)
    {
        try
        {
            var key = $"{USER_CONNECTION_KEY_PREFIX}{userId}";
            // User connections expire after 1 hour of inactivity
            var expiry = TimeSpan.FromHours(1);
            return await _db.StringSetAsync(key, connectionId, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving user connection for {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Get user's connection ID
    /// </summary>
    public async Task<string?> GetUserConnectionAsync(string userId)
    {
        try
        {
            var key = $"{USER_CONNECTION_KEY_PREFIX}{userId}";
            var connectionId = await _db.StringGetAsync(key);
            return connectionId.IsNullOrEmpty ? null : connectionId.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user connection for {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Remove user connection mapping
    /// </summary>
    public async Task<bool> RemoveUserConnectionAsync(string userId)
    {
        try
        {
            var key = $"{USER_CONNECTION_KEY_PREFIX}{userId}";
            return await _db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user connection for {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Get all online user IDs (for compatibility)
    /// </summary>
    public async Task<List<string>> GetOnlineUsersAsync()
    {
        try
        {
            var server = _db.Multiplexer.GetServer(_db.Multiplexer.GetEndPoints().First());
            var keys = server.Keys(pattern: $"{USER_CONNECTION_KEY_PREFIX}*").ToList();
            
            return keys.Select(k => k.ToString().Replace(USER_CONNECTION_KEY_PREFIX, "")).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting online users");
            return new List<string>();
        }
    }

    /// <summary>
    /// Get all active encounters
    /// </summary>
    public async Task<List<EncounterData>> GetAllActiveEncountersAsync()
    {
        try
        {
            var server = _db.Multiplexer.GetServer(_db.Multiplexer.GetEndPoints().First());
            var keys = server.Keys(pattern: $"{ENCOUNTER_KEY_PREFIX}*").ToList();
            
            var encounters = new List<EncounterData>();
            foreach (var key in keys)
            {
                var json = await _db.StringGetAsync(key);
                if (!json.IsNullOrEmpty)
                {
                    var encounter = JsonSerializer.Deserialize<EncounterData>(json!);
                    if (encounter != null && (encounter.Status == CallStatus.Connected || encounter.Status == CallStatus.Ringing || encounter.Status == CallStatus.Initiated))
                    {
                        encounters.Add(encounter);
                    }
                }
            }
            
            return encounters;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all active encounters");
            return new List<EncounterData>();
        }
    }

    /// <summary>
    /// Get user IDs who are currently on active calls
    /// </summary>
    public async Task<HashSet<string>> GetUsersOnCallAsync()
    {
        try
        {
            var encounters = await GetAllActiveEncountersAsync();
            var usersOnCall = new HashSet<string>();
            
            foreach (var encounter in encounters)
            {
                usersOnCall.Add(encounter.CallerId);
                usersOnCall.Add(encounter.CalleeId);
            }
            
            return usersOnCall;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users on call");
            return new HashSet<string>();
        }
    }
}

/// <summary>
/// Data model for encounter information stored in Redis
/// </summary>
public class EncounterData
{
    public string CallId { get; set; } = string.Empty;
    public string EncounterId { get; set; } = string.Empty;
    public string CallerId { get; set; } = string.Empty;
    public string CallerName { get; set; } = string.Empty;
    public string CalleeId { get; set; } = string.Empty;
    public string CalleeName { get; set; } = string.Empty;
    public string CaseId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public CallType CallType { get; set; }
    public CallStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? ConnectedTime { get; set; }
    public DateTime? EndTime { get; set; }
    public DateTime? DisconnectedTime { get; set; }
}
