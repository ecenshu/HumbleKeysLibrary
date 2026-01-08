using System;
using System.Threading;
using System.Threading.Tasks;
using HumbleKeys.Models;
using HumbleKeys.Services;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Data;
using Order = HumbleKeys.Services.GameKey.Models.Order;

namespace HumbleKeys.ChainHandlers.OrderHandlers
{
    public class DbCacheOrderHandler : OrderHandlerBase
    {
        private readonly IHumbleOrderRepository humbleOrderRepository;
        private readonly ILogger logger;
        
        public DbCacheOrderHandler(IHumbleOrderRepository humbleOrderRepository, ILogger logger)
        {
            this.humbleOrderRepository = humbleOrderRepository;
            this.logger = logger;
        }
        
        // If an order exists in the database for this gamekey,
        // ensure that the record is not stale,
        // if it is already complete (no more valid keys to redeem/claim from humble) then return the db entry;
        // otherwise, check the order against the next OrderHandler
        public override async Task<IOrder> ExecuteAsync(string gameKey, CancellationToken cancellationToken = default)
        {
            var order = await humbleOrderRepository.GetOrderAsync(gameKey, cancellationToken: cancellationToken);
            if (order == null || !order.ContainsProcessableKeyStatuses()/* || orderRecord.choices_remaining <= orderRecord.total_choices*/)
            {
                var nextOrder = await next.ExecuteAsync(gameKey, cancellationToken);
                
                if (nextOrder == null) return order;
                if (order != null)
                {
                    // update order from newOrder
                    if (order is Order updatableOrder)
                    {
                        updatableOrder.UpdateValues(nextOrder);
                    }
                }
                else
                {
                    order = new Order(nextOrder);
                }

                humbleOrderRepository.Update(order);
            }
            return order;
        }
    }
}