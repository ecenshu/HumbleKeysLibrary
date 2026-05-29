using System.Threading;
using System.Threading.Tasks;

namespace HumbleKeys.ChainHandlers
{
    public interface IChainHandler<TResult,T>
    {
        IChainHandler<TResult,T> SetNextHandler(IChainHandler<TResult,T> nextHandler);
        Task<TResult> ExecuteAsync(T gameKey, CancellationToken cancellationToken = default);
    }

    public interface IChainHandler<TResult>
    {
        IChainHandler<TResult> SetNextHandler(IChainHandler<TResult> nextHandler);
        Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default);
    }
    
    public interface IAsyncChainHandler<TResult>
    {
        IChainHandler<TResult> SetNextHandler(IChainHandler<TResult> nextHandler);
        Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}