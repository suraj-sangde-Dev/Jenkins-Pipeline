using StackExchange.Redis;

namespace TeleICU.API.Helpers
{
    public class RedisOtpStore
    {
        private readonly IDatabase _db;

        public RedisOtpStore(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task StoreOtpAsync(string email, string otp, TimeSpan expiry)
        {
            await _db.StringSetAsync($"otp:{email}", otp, expiry);
        }

        public async Task<string?> GetOtpAsync(string email)
        {
            return await _db.StringGetAsync($"otp:{email}");
        }

        public async Task RemoveOtpAsync(string email)
        {
            await _db.KeyDeleteAsync($"otp:{email}");
        }
    }
}