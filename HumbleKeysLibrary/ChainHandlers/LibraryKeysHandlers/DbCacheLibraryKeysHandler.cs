using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HumbleKeys.Services;
using Playnite.SDK;

namespace HumbleKeys.ChainHandlers.LibraryKeysHandlers
{
    public class DbCacheLibraryKeysHandler : LibraryKeysHandler
    {
        private readonly IHumbleOrderRepository humbleOrderRepository;
        private readonly ILogger logger;

        public DbCacheLibraryKeysHandler(IHumbleOrderRepository humbleOrderRepository, ILogger logger)
        {
            this.humbleOrderRepository = humbleOrderRepository;
            this.logger = logger;
        }

        public override async Task<IEnumerable<string>> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (next == null) return Array.Empty<string>();
            
            var libraryKeys = await next.ExecuteAsync(cancellationToken);
            
            // Filter out based on database entries
            try
            {
                var completeOrderKeys = humbleOrderRepository.GetCompleteOrderKeys();
                if (completeOrderKeys != null && libraryKeys != null)
                {
                    return libraryKeys.Except(completeOrderKeys);
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to get complete order keys");
            }
            return libraryKeys;
        }
    }
}