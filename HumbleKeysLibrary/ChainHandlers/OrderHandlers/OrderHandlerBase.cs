using System.Threading;
using System.Threading.Tasks;
using HumbleKeys.ChainHandlers.LibraryKeysHandlers;
using HumbleKeys.Models;
using HumbleKeys.Services;
using Playnite.SDK;

namespace HumbleKeys.ChainHandlers.OrderHandlers
{
    /// <summary>
    /// Get an Order from the persistent repositories preferring local data over remote data
    /// </summary>
    public abstract class OrderHandlerBase : IChainHandler<IOrder,string>
    {
        protected IChainHandler<IOrder,string> next;

        public IChainHandler<IOrder,string> SetNextHandler(IChainHandler<IOrder,string> nextHandler)
        {
            next = nextHandler;
            return next;
        }

        public abstract Task<IOrder> ExecuteAsync(string gameKey, CancellationToken cancellationToken = default);
    }
}