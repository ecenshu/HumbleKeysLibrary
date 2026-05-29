using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HumbleKeys.Services;
using Playnite.SDK;

namespace HumbleKeys.ChainHandlers.LibraryKeysHandlers
{
    public class RemoteLibraryKeysHandler : LibraryKeysHandler
    {
        private readonly HumbleOrderApiRepository client;
        private readonly ILogger logger;

        
        public RemoteLibraryKeysHandler(HumbleOrderApiRepository client, ILogger logger)
        {
            this.client = client;
            this.logger = logger;
        }

        public override async Task<IEnumerable<string>> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return client.GetLibraryKeys();
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to retrieve remote library keys");
            }
            return Array.Empty<string>();
        }
    }
}