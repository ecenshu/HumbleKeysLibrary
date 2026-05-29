using System;
using System.Threading;
using System.Threading.Tasks;
using HumbleKeys.Services;
using Playnite.SDK;
using HumbleKeys.Models;
using Path = System.IO.Path;

namespace HumbleKeys.ChainHandlers.OrderHandlers
{
    public class FileCacheGetOrderHandler : OrderHandlerBase
    {
        private readonly IFileCacheProvider fileCacheProvider;
        private readonly ILogger logger;

        public FileCacheGetOrderHandler(IFileCacheProvider fileCacheProvider, ILogger logger)
        {
            this.fileCacheProvider = fileCacheProvider;
            this.logger = logger;
        }
        
        public override async Task<IOrder> ExecuteAsync(string gameKey, CancellationToken cancellationToken = default)
        {
            IOrder order = null;

            var cacheFileName = Path.Combine(fileCacheProvider.LocalCachePath,"order",$"{gameKey}.json");
            try
            {
                var rawOrder = await fileCacheProvider.GetCacheContentAsync(cacheFileName, cancellationToken);
                if (!string.IsNullOrEmpty(rawOrder))
                {
                    order = Order.FromJson(rawOrder);
                }
                if (order != null && (order.ContainsProcessableKeyStatuses() || fileCacheProvider.CacheEnabled))
                    return order;

                var nextOrder = await next.ExecuteAsync(gameKey, cancellationToken);
                if (nextOrder == null) return order;
                
                if (fileCacheProvider.CacheEnabled) fileCacheProvider.CreateCacheContent(cacheFileName, nextOrder.Buffer);
                return nextOrder;

            }
            catch (Exception e)
            {
                logger.Error(e, e.Message);
                if (next != null) order = await next.ExecuteAsync(gameKey, cancellationToken);
            }
            if (order != null)
            {
                fileCacheProvider.CreateCacheContent(cacheFileName, order.Buffer);
            }    
            return order;   
        }
    }
}