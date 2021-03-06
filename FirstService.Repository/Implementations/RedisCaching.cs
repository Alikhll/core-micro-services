﻿using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace FirstService.Repository.Implementations
{
    public interface IRedisCaching<T>
    {
        Task<T> GetCachedData(string key);
        Task SetCachedData(string key, T value);
    }

    /*
     *  distributed cache service
     */
    public class RedisCaching<T> : IRedisCaching<T>
    {
        public IDistributedCache _distributedCache { get; }

        public RedisCaching(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }

        public async Task<T> GetCachedData(string key)
        {
            var data = await _distributedCache.GetStringAsync(key);

            if (!string.IsNullOrEmpty(data))
            {
                T obj = JsonConvert.DeserializeObject<T>(data);
                return obj;
            }

            return default(T);
        }

        public Task SetCachedData(string key, T value)
        {
            string data = JsonConvert.SerializeObject(value);
            _distributedCache.SetStringAsync(key, data);
            return Task.CompletedTask;
        }
    }
}
