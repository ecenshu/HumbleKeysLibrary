using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HumbleKeys.Services;
using Playnite.SDK;
using Playnite.SDK.Data;

namespace HumbleKeys.ChainHandlers.LibraryKeysHandlers
{
    public class FileCacheLibraryKeysHandler : LibraryKeysHandler
    {
        private readonly IFileCacheProvider fileCacheProvider;
        private readonly ILogger logger;

        public FileCacheLibraryKeysHandler(IFileCacheProvider fileCacheProvider, ILogger logger)
        {
            this.fileCacheProvider = fileCacheProvider;
            this.logger = logger;
        }

        public override async Task<IEnumerable<string>> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var keysCacheFilename = $"{fileCacheProvider.LocalCachePath}\\gamekeys.json";

            // Request may be cached in the local filesystem to prevent spamming Humble
            try
            {
                IEnumerable<string> libraryKeys;
                if (fileCacheProvider.CacheEnabled)
                {
                    libraryKeys = await fileCacheProvider.GetCacheContentAsync<IEnumerable<string>>(keysCacheFilename, cancellationToken);
                    if (libraryKeys != null) return libraryKeys;
                }

                if (next == null) return Array.Empty<string>();
                
                libraryKeys = await next.ExecuteAsync(cancellationToken);
                if (libraryKeys != null)
                {
                    fileCacheProvider.CreateCacheContent(keysCacheFilename, Serialization.ToJson(libraryKeys, false));
                }

                return libraryKeys;
            }
            catch (Exception e)
            {
                logger.Error(e, e.Message);
            }

            if (next != null) return await next.ExecuteAsync(cancellationToken);
            
            return Array.Empty<string>();
        }
    }
}