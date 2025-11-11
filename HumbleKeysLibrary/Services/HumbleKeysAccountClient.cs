using HumbleKeys.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK.Data;

namespace HumbleKeys.Services
{
    public class HumbleKeysAccountClient : IDataProvider<OrderKey, Order>, IDataProvider<ICollection<OrderKey>>
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private readonly IWebView webView;
        private const string loginUrl = @"https://www.humblebundle.com/login?goto=%2Fhome%2Flibrary&qs=hmb_source%3Dnavbar";
        private const string libraryUrl = @"https://www.humblebundle.com/home/library?hmb_source=navbar";
        private const string logoutUrl = @"https://www.humblebundle.com/logout?goto=/";
        private const string orderUrlMask = @"https://www.humblebundle.com/api/v1/order/{0}?all_tpkds=true";

        const string subscriptionCategory = @"subscriptioncontent";
        readonly bool preferCache;
        readonly string localCachePath;

        public HumbleKeysAccountClient(IWebView webView)
        {
            this.webView = webView;
        }

        public HumbleKeysAccountClient(IWebView webView, IHumbleKeysAccountClientSettings clientSettings) : this(webView)
        {
            localCachePath = Directory.Exists(clientSettings.CachePath) ? clientSettings.CachePath : new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;

            preferCache = clientSettings.CacheEnabled;
            // initialise folder structure for local cache
            var cachePaths = new[] { "order", "membership/v2", "membership/v3", "membership" };
            if (preferCache)
            {
                foreach (var cachePath in cachePaths)
                {
                    if (!Directory.Exists($"{localCachePath}\\{cachePath}"))
                    {
                        Directory.CreateDirectory($"{localCachePath}\\{cachePath}");
                    }
                }

                logger.Info("Cache directories prepared");
            }
            else
            {
                File.Delete($"{localCachePath}\\gameKeys.json");
                foreach (var cachePath in cachePaths)
                {
                    if (!Directory.Exists($"{localCachePath}\\{cachePath}")) continue;

                    var cachedFiles = Directory.EnumerateFiles($"{localCachePath}\\{cachePath}");
                    foreach (var cachedFile in cachedFiles)
                    {
                        File.Delete(cachedFile);
                    }

                    Directory.Delete($"{localCachePath}\\{cachePath}");
                }

                logger.Info("Cache cleared");
            }
        }

        public void Login()
        {
            //webView.NavigationChanged += (s, e) =>
            webView.LoadingChanged += (s, e) =>
            {
                if (webView.GetCurrentAddress() == libraryUrl)
                {
                    webView.Close();
                }
            };

            webView.DeleteDomainCookies(".humblebundle.com");
            webView.DeleteDomainCookies("www.humblebundle.com");
            webView.Navigate(loginUrl);
            webView.OpenDialog();
        }

        public bool GetIsUserLoggedIn()
        {
            webView.NavigateAndWait(libraryUrl);
            return webView.GetPageSource().Contains("\"gamekeys\":");
        }

        /// Retrieves all gameKeys associated with the current logged in account
        internal async Task<IEnumerable<OrderKey>> GetLibraryKeysAsync(CancellationToken token = default)
        {
            logger.Info("Fetching library keys from Humble Bundle");
            var strKeys = await ScrapeLibraryKeys(libraryUrl, @"""gamekeys"":\s*(\[.+\])");
            var parsedLibraryKeys = Serialization.FromJson<List<OrderKey>>(strKeys);
            logger.Trace($"Request:{libraryUrl} Content:{Serialization.ToJson(parsedLibraryKeys, true)}");

            return parsedLibraryKeys;
        }

        private async Task<string> ScrapeLibraryKeys(string url, string dataMatcher)
        {
            webView.NavigateAndWait(url);
            var libSource = await webView.GetPageSourceAsync();
            var match = Regex.Match(libSource, dataMatcher);
            if (!match.Success) throw new Exception("User is not authenticated.");

            var strKeys = match.Groups[1].Value;
            return strKeys;
        }

        async Task<string> GetCacheContentAsync(string keysCacheFilename, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(keysCacheFilename)) return null;
            using (var streamReader = new StreamReader(new FileStream(keysCacheFilename, FileMode.Open)))
            {
                var cacheContent = await streamReader.ReadToEndAsync();
                streamReader.Close();

                return cancellationToken.IsCancellationRequested ? string.Empty : cacheContent;
            }
        }

        async Task<T> GetCacheContentAsync<T>(string keysCacheFilename, CancellationToken cancellationToken = default) where T : class
        {
            var cacheContent = await GetCacheContentAsync(keysCacheFilename, cancellationToken);
            return cacheContent == null ? null : Serialization.FromJson<T>(cacheContent);
        }

        void CreateCacheContent(string cacheFilename, string strCacheEntry)
        {
            var streamWriter = new StreamWriter(new FileStream(cacheFilename, FileMode.OpenOrCreate));
            streamWriter.Write(strCacheEntry);
            streamWriter.Close();
        }

        internal async Task<ICollection<Order>> GetOrdersAsync(ICollection<OrderKey> gameKeys, bool includeChoiceMonths = false, CancellationToken cancellationToken = default)
        {
            var orders = new List<Order>();
            var gameKeysList = gameKeys.ToList();
            logger.Trace($"GetOrders: Processing {gameKeysList.Count} game keys");

            foreach (var key in gameKeysList)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var order = await GetOrderAsync(key, cancellationToken);

                orders.Add(order);
                logger.Trace($"GetOrders: Added order {order.gamekey} with {order.tpkd_dict.all_tpks.Count} total tpks");
            }

            logger.Trace($"GetOrders: Completed processing {orders.Count} orders");
            return orders;
        }

        /// <summary>
        /// Alters order by adding tpkd entries from the Choice/Month which have not been claimed to the source order to the current order as tkpd virtual entries
        /// </summary>
        /// <param name="order"></param>
        public async Task AddChoiceMonthlyGamesAsync(Order order, CancellationToken cancellationToken = default)
        {
            string versionCachePath;
            if (order.product.is_subs_v2_product)
            {
                versionCachePath = "v2";
            }
            else if (order.product.is_subs_v3_product)
            {
                versionCachePath = "v3";
            }
            else
            {
                versionCachePath = "unknown";
            }

            var cachePath = $"membership/{versionCachePath}/{order.product.choice_url}";
            // if the monthly choice_url can be parsed, store the cache file in a ISO YYYY-MM file instead
            if (DateTime.TryParse(order.product.choice_url, out var choiceDate))
            {
                cachePath = $"membership/{versionCachePath}/{choiceDate:yyyy-MM}";
            }

            var choiceUrl = $"https://www.humblebundle.com/membership/{order.product.choice_url}";
            var strChoiceMonth = string.Empty;

            var orderCacheFilename = $"{localCachePath}/{cachePath}.json";
            var cacheHit = false;
            if (preferCache)
            {
                // Request may be cached in local filesystem to prevent spamming Humble
                strChoiceMonth = await GetCacheContentAsync(orderCacheFilename, cancellationToken);
                cacheHit = !string.IsNullOrEmpty(strChoiceMonth);
            }

            if (string.IsNullOrEmpty(strChoiceMonth))
            {
                webView.NavigateAndWait(choiceUrl);
                var match = Regex.Match(await webView.GetPageSourceAsync(),
                    @"<script id=""webpack-monthly-product-data"" type=""application/json"">([\s\S]*?)</script>");
                if (match.Success)
                {
                    strChoiceMonth = match.Groups[1].Value;
                    if (preferCache)
                    {
                        // save data into cache
                        CreateCacheContent(orderCacheFilename, strChoiceMonth);
                    }
                }
                else
                {
                    logger.Error($"Unable to obtain Choice Monthly data for entry [{order.product.choice_url}]");
                }
            }

            if (string.IsNullOrEmpty(strChoiceMonth)) return;

            IChoiceMonth choiceMonth = null;
            if (order.product.is_subs_v2_product)
            {
                choiceMonth = Serialization.FromJson<ChoiceMonthV2>(strChoiceMonth);
                logger.Trace($"Request:{choiceUrl} {(cacheHit ? "From Cache " : "")}Content:{Serialization.ToJson(choiceMonth, true)}");
            }
            else if (order.product.is_subs_v3_product)
            {
                choiceMonth = Serialization.FromJson<ChoiceMonthV3>(strChoiceMonth);
                logger.Trace($"Request:{choiceUrl} {(cacheHit ? "From Cache " : "")}Content:{Serialization.ToJson(choiceMonth, true)}");
            }
            else
            {
                logger.Error("Unknown Choice Monthly product version");
            }

            if (choiceMonth == null) return;

            // Add contentChoice to all_tpks if it doesn't already exist (all_tpks gets populated by the order if it is already redeemed)
            // Only add to the order if the month contains redeemable games, may already have exhausted the selection count
            var orderMachineNames = order.tpkd_dict.all_tpks.Select(tpk => tpk.machine_name).ToList();

            var contentChoicesNotInOrder = choiceMonth.ContentChoices.Keys.ToList().Where(contentChoiceKey => !choiceMonth.ChoicesMade.Contains(contentChoiceKey));
            foreach (var contentChoiceKey in contentChoicesNotInOrder)
            {
                // get tkpds either directly or via nested_choice_tpkds
                Order.TpkdDict.Tpk[] orderEntries = null;
                var contentChoice = choiceMonth.ContentChoices[contentChoiceKey];
                if (contentChoice.tpkds != null)
                {
                    orderEntries = contentChoice.tpkds;
                }
                else if (contentChoice.nested_choice_tpkds != null)
                {
                    var nestedOrderEntries = new List<Order.TpkdDict.Tpk>();
                    foreach (var nestedChoiceTpkd in contentChoice.nested_choice_tpkds)
                    {
                        nestedOrderEntries.AddRange(nestedChoiceTpkd.Value);
                    }

                    orderEntries = nestedOrderEntries.ToArray();
                }
                else
                {
                    logger.Error($"Unable to retrieve tpkds for Choice Month Title:{choiceMonth.Title}");
                }

                if (orderEntries == null) continue;

                foreach (var contentChoiceTpkd in orderEntries)
                {
                    contentChoiceTpkd.is_virtual = true;
                }

                order.tpkd_dict.all_tpks.AddRange(orderEntries);
            }
        }

        public async Task<Order> GetOrderAsync(OrderKey key, CancellationToken cancellationToken)
        {
            var orderUri = string.Format(orderUrlMask, key);
            var cacheFileName = $"{localCachePath}/order/{key}.json";
            Order order = null;
            bool cacheHit;
            if (preferCache)
            {
                order = await GetCacheContentAsync<Order>(cacheFileName, cancellationToken);
            }

            if (order == null)
            {
                cacheHit = false;
                logger.Trace($"Fetching order details");
                webView.NavigateAndWait(orderUri);
                var strContent = await webView.GetPageTextAsync();
                if (preferCache)
                {
                    CreateCacheContent(cacheFileName, strContent);
                }

                order = Serialization.FromJson<Order>(strContent);
            }
            else
            {
                cacheHit = true;
            }

            logger.Trace($"Request:{orderUri} {(cacheHit ? "Cached " : "")}Content:{Serialization.ToJson(order, true)}");
            return order;
        }

        public IEnumerable<Order> GetData(ICollection<OrderKey> keys)
        {
            return keys.Select(GetData);
        }

        public async Task<Order> GetDataAsync(OrderKey key, CancellationToken cancellationToken = default)
        {
            return await GetOrderAsync(key, cancellationToken);
        }

        public Order GetData(OrderKey key)
        {
            return GetDataAsync(key, CancellationToken.None).Result;
        }

        public async Task<IEnumerable<Order>> GetDataAsync(IEnumerable<OrderKey> keys, CancellationToken token)
        {
            var orders = new List<Order>();
            foreach (var key in keys)
            {
                orders.Add(await GetOrderAsync(key, token));
            }

            return orders;
        }

        public ICollection<OrderKey> GetData()
        {
            return GetDataAsync(CancellationToken.None).Result;
        }

        public async Task<ICollection<OrderKey>> GetDataAsync(CancellationToken token)
        {
            return await GetLibraryKeysAsync(token) as ICollection<OrderKey>;
        }
    }

    public interface IDataProvider<T, TResult>
    {
        IEnumerable<TResult> GetData(ICollection<T> keys);
        TResult GetData(T key);

        Task<IEnumerable<TResult>> GetDataAsync(IEnumerable<T> keys, CancellationToken token);
        Task<TResult> GetDataAsync(T key, CancellationToken token);
    }

    public interface IDataProvider<TResult>
    {
        TResult GetData();
        Task<TResult> GetDataAsync(CancellationToken token);
    }
}
