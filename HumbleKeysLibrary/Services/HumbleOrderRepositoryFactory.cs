using Playnite.SDK;

namespace HumbleKeys.Services
{
    public abstract class HumbleOrderRepositoryFactory
    {
        /// <summary>
        ///  Composition Root
        /// </summary>
        /// <param name="webView"></param>
        /// <param name="settings"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static IHumbleOrderRepository Create(IWebView webView, IHumbleKeysAccountClientSettings settings, ILogger logger)
        {
            var dbRepository = new HumbleOrderSqlRepository(settings, logger);
            
            var humbleKeysAccountClient = new HumbleKeysAccountClient(webView, settings, logger);
            var fileCacheRepository = new HumbleOrderFileSystemRepository(settings, logger);
            
            var apiRepository = new HumbleOrderApiRepository(humbleKeysAccountClient);
            var humbleOrderCachedRepository = new HumbleOrderCachedRepository(fileCacheRepository, apiRepository, settings.CacheEnabled, logger);
            var cachedRepository = new HumbleOrderCachedRepository(dbRepository, humbleOrderCachedRepository, false, logger);
            
            // todo: Fix circular dependency?
            humbleKeysAccountClient.SetRepository(cachedRepository);
            
            return cachedRepository;
        }
    }
}