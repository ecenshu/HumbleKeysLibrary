using System.Threading;
using System.Threading.Tasks;
using HumbleKeys.Models;
using HumbleKeys.Services;

namespace HumbleKeys.ChainHandlers.OrderHandlers
{
    public class RemoteOrdersHandler : OrderHandlerBase
    {
        private readonly HumbleOrderApiRepository api;

        public RemoteOrdersHandler(HumbleOrderApiRepository api)
        {
            this.api = api;
        }

        public override async Task<IOrder> ExecuteAsync(string gameKey, CancellationToken cancellationToken = default)        {
            var order = await api.GetOrderAsync(gameKey, cancellationToken: cancellationToken);
            if (order == null || order.ContainsProcessableKeyStatuses() && next != null) return await next.ExecuteAsync(gameKey, cancellationToken);
            
            return order;
        }
    }
}