using Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Infrastructure.Redis.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(IDistributedCache cache)
    {
        _cache = cache;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var cachedData = await _cache.GetStringAsync(key, cancellationToken);
        if (string.IsNullOrEmpty(cachedData))
            return default;

        return JsonSerializer.Deserialize<T>(cachedData, _jsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? slidingExpiration = null, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default)
    {
        var serializedData = JsonSerializer.Serialize(value, _jsonOptions);

        var options = new DistributedCacheEntryOptions();

        // Sliding: Hết hạn nếu KHÔNG ai truy cập sau 1 khoảng thời gian
        if (slidingExpiration.HasValue) options.SetSlidingExpiration(slidingExpiration.Value);

        // Absolute: Cứng rắn hết hạn sau 1 khoảng thời gian dù có ai truy cập hay không
        if (absoluteExpiration.HasValue) options.SetAbsoluteExpiration(absoluteExpiration.Value);

        await _cache.SetStringAsync(key, serializedData, options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? slidingExpiration = null, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default)
    {
        // 1. Thử lấy từ Cache
         var cachedData = await _cache.GetStringAsync(key, cancellationToken);
        // 2. Nếu chuỗi CÓ TỒN TẠI (kể cả nó là chữ "false") -> Đó là Cache Hit
        if (!string.IsNullOrEmpty(cachedData ) && cachedData != "false")
        {
            return JsonSerializer.Deserialize<T>(cachedData, _jsonOptions)!;
        }
        // 2. Nếu Cache miss, gọi hàm Factory (thường là truy vấn DB)
        var newValue = await factory(cancellationToken);

        // 3. Nếu DB có dữ liệu, lưu vào Cache
        if (newValue != null)
        {
            await SetAsync(key, newValue, slidingExpiration, absoluteExpiration, cancellationToken);
        }

        return newValue!;
    }

    public async Task RemoveByPrefixAsync(string prefixKey, CancellationToken cancellationToken = default)
    {
        // Với IDistributedCache thuần, việc xóa theo prefix hơi phức tạp. 
        // Trong tương lai nếu bạn dùng StackExchange.Redis IServer, bạn có thể gọi server.Keys(pattern: prefixKey + "*")
        // Tạm thời, ta thiết kế interface để sẵn sàng cho tương lai.
        throw new NotImplementedException("Cần tiêm IConnectionMultiplexer để quét Keys theo pattern");
    }
}