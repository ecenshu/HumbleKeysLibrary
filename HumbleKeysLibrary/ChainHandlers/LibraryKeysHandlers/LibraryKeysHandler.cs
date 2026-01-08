using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HumbleKeys.ChainHandlers.LibraryKeysHandlers
{
    public abstract class LibraryKeysHandler: IChainHandler<IEnumerable<string>>
    {
        protected IChainHandler<IEnumerable<string>> next = null;

        public IChainHandler<IEnumerable<string>> SetNextHandler(IChainHandler<IEnumerable<string>> nextHandler)
        {
            next = nextHandler;
            return next;
        }

        public abstract Task<IEnumerable<string>> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}