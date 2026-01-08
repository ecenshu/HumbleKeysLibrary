using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HumbleKeys.Models;
using HumbleKeys.Services.GameKey.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using Order = HumbleKeys.Services.GameKey.Models.Order;

namespace HumbleKeys.Services
{
    public class HumbleOrderFileSystemRepository : IHumbleOrderRepository
    {
        private readonly IHumbleKeysAccountClientSettings settings;
        private readonly FileCacheProvider provider;
        private const string choicePath = "membership";
        private const string orderPath = "order";

        public HumbleOrderFileSystemRepository(IHumbleKeysAccountClientSettings settings, ILogger logger)
        {
            this.settings = settings;
            provider = new FileCacheProvider(settings, logger);
        }
        public void Dispose()
        {
            
        }

        public bool Update(Product product)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetLibraryKeys()
        {
            return null;
        }

        public async Task<IEnumerable<string>> GetLibraryKeysAsync(CancellationToken cancellationToken = default)
        {
            return null;
        }

        public IEnumerable<IOrder> FilterOrders(string[] orderIds)
        {
            throw new NotImplementedException();
        }

        public bool Update(ITpk record)
        {
            throw new NotImplementedException();
        }

        public IOrder GetOrder(string id, bool retrieveLinkedRecords = false)
        {
            return GetOrderAsync(id, retrieveLinkedRecords).Result;
        }

        public async Task<IOrder> GetOrderAsync(string id, bool retrieveLinkedRecords = false, CancellationToken cancellationToken = default)
        {
            if (!provider.CacheEnabled) return null;

            var cacheFilePath = Path.Combine(settings.CachePath, orderPath, $"{id}.json");
            var order =  await provider.GetCacheContentAsync<Models.Order>(cacheFilePath, cancellationToken);
            if (retrieveLinkedRecords && order.IsChoiceOrder())
            {
                // todo: Fill in humble choice data
                var humbleChoiceAsync = await GetHumbleChoiceAsync(order, cancellationToken);
            }

            return order;
        }

        public Product GetProductById(string id)
        {
            throw new NotImplementedException();
        }

        public bool Update(IOrder order)
        {
            var cacheFilePath = Path.Combine(settings.CachePath, orderPath, $"{order.gamekey}.json");
            provider.CreateCacheContent(cacheFilePath, Serialization.ToJson(order));
            var fileInfo = new FileInfo(cacheFilePath);
            return fileInfo.Exists && fileInfo.Length>0;
        }

        public bool Update(SubProduct orderSubproduct)
        {
            throw new NotImplementedException();
        }

        public bool Update(Download download)
        {
            throw new NotImplementedException();
        }

        public bool Update(Download.DownloadStruct downloadStruct)
        {
            throw new NotImplementedException();
        }

        public bool Update(Order.TpkdDict tpkdDict)
        {
            throw new NotImplementedException();
        }

        public bool Update(Order.TpkdDict.Tpk tpk)
        {
            throw new NotImplementedException();
        }

        public bool IsStale()
        {
            throw new NotImplementedException();
        }

        public bool IsUnprocessedOrders(string orderId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetCompleteOrderKeys()
        {
            throw new NotImplementedException();
        }
        
        public IChoiceMonth GetHumbleChoice(IOrder order)
        {
            return GetHumbleChoiceAsync(order, CancellationToken.None).Result;
        }

        public async Task<IChoiceMonth> GetHumbleChoiceAsync(IOrder order, CancellationToken cancellationToken = default)
        {
            if (!provider.CacheEnabled) return null;

            var cacheFilePath = string.Empty;
            var choiceVersion = order.product.is_subs_v2_product ? "v2" : order.product.is_subs_v3_product ? "v3" : string.Empty;
            if (DateTime.TryParse(order.product.choice_url, out var choiceDate))
            {
                cacheFilePath = Path.Combine(settings.CachePath, choicePath, choiceVersion, $"{choiceDate:yyyy-MM}.json");
            }

            switch (choiceVersion)
            {
                case "v2":
                {
                    return await provider.GetCacheContentAsync<ChoiceMonthV2>(cacheFilePath, cancellationToken);
                }
                case "v3":
                {
                    return await provider.GetCacheContentAsync<ChoiceMonthV3>(cacheFilePath, cancellationToken);
                }
                default:
                    throw new NotSupportedException("Indeterminate Choice Version");
            }
        }

        public void Update(IOrder order, IChoiceMonth sourceChoiceMonth)
        {
            string versionCachePath = string.Empty;
            if (sourceChoiceMonth is ChoiceMonthV2)
            {
                versionCachePath = "v2";
            }

            if (sourceChoiceMonth is ChoiceMonthV3)
            {
                versionCachePath = "v3";
            }
            var cachePath = $"membership/{versionCachePath}/{sourceChoiceMonth.Title}";
            if (DateTime.TryParse(order.product.choice_url, out var choiceDate))
            {
                cachePath = $"membership/{versionCachePath}/{choiceDate:yyyy-MM}";
            }
            provider.CreateCacheContent(cachePath, Serialization.ToJson(sourceChoiceMonth));
        }
    }
}