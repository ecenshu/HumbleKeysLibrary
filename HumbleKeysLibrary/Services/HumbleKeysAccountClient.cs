using HumbleKeys.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK.Data;

namespace HumbleKeys.Services
{
    public class HumbleKeysAccountClient : IHumbleOrdersProvider, IOrdersProvider, IDisposable
    {
        private IHumbleOrderRepository orderRepository;

        public void SetRepository(IHumbleOrderRepository repository)
        {
            orderRepository = repository;
        }
        
        private readonly IHumbleKeysAccountClientSettings clientSettings;
        private readonly ILogger logger;
        private readonly IWebView webView;
        private const string loginUrl = @"https://www.humblebundle.com/login?goto=%2Fhome%2Flibrary&qs=hmb_source%3Dnavbar";
        private const string libraryUrl = @"https://www.humblebundle.com/home/library?hmb_source=navbar";
        private const string logoutUrl = @"https://www.humblebundle.com/logout?goto=/";
        private const string orderUrlMask = @"https://www.humblebundle.com/api/v1/order/{0}?all_tpkds=true";

        private const string subscriptionCategory = @"subscriptioncontent";
        private readonly bool preferCache;

        public HumbleKeysAccountClient(IWebView webView)
        {
            this.webView = webView;
            logger = LogManager.GetLogger();
        }

        public HumbleKeysAccountClient(IWebView webView, IHumbleKeysAccountClientSettings clientSettings) : this(webView)
        {
            this.clientSettings = clientSettings;
            preferCache = clientSettings.CacheEnabled;
        }

        public HumbleKeysAccountClient(IWebView webView, IHumbleKeysAccountClientSettings clientSettings, ILogger logger) : this(webView, clientSettings)
        {
            this.logger = logger;
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

        /// <summary>
        /// Return a list of gamekeys which may have unclaimed/unredeemed keys
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<List<string>> GetLibraryKeysAsync(CancellationToken token = default)
        {
            logger.Trace("Fetching library keys from Humble Bundle");
            webView.NavigateAndWait(libraryUrl);
            var libSource = await webView.GetPageSourceAsync();
            var match = Regex.Match(libSource, @"""gamekeys"":\s*(\[.+\])");
            if (!match.Success) throw new Exception("User is not authenticated.");

            var strKeys = match.Groups[1].Value;
            var libraryKeys = Serialization.FromJson<List<string>>(strKeys);
            logger.Trace($"Request:{libraryUrl} Content:{Serialization.ToJson(libraryKeys, true)}");

            return libraryKeys;
        }
        
        internal Dictionary<string, Models.Order> GetOrders(IEnumerable<string> gameKeys)
        {
            var orders = new Dictionary<string, Models.Order>();
            var gameKeysList = gameKeys.ToList();
            logger.Trace($"GetOrders: Processing {gameKeysList.Count} game keys");

            foreach (var key in gameKeysList)
            {
                var order = GetOrder(key);
                orders.Add(order.gamekey, order);
                logger.Trace($"GetOrders: Added order {order.gamekey} with {order.tpkd_dict.all_tpks.Count} total tpks");
            }

            logger.Trace($"GetOrders: Completed processing {orders.Count} orders");
            return orders;
        }

        public Models.Order GetOrder(string gameKey)
        {
            var orderUri = string.Format(orderUrlMask, gameKey); 

            logger.Trace("Fetching order details");
            webView.NavigateAndWait(orderUri);
            var strContent = webView.GetPageText();
            var order = Serialization.FromJson<Models.Order>(strContent);
            order.Buffer = strContent;
            
            /*if (string.Equals(order.product.category, subscriptionCategory, StringComparison.Ordinal) && !string.IsNullOrEmpty(order.product.choice_url) && clientSettings.ImportChoiceKeys)
            {
                var choiceMonth = GetChoiceMonthlyGames(order);
                var orderMergedWithChoiceGames = MergeMonthlyGames(ref order, choiceMonth);
            }*/
            /*else
            {
                var unredeemedKeys = (from tpkd in order.tpkd_dict.all_tpks where tpkd.key_type == "steam" && tpkd.redeemed_key_val == null && !tpkd.is_expired select tpkd.machine_name).ToList();
                if (!unredeemedKeys.Any())
                {
                    // todo: Handle generic key like gamekey : hHtUWebub6nz2SDn
                    // Blacklist gamekey from being retrieved again?
                }
            }*/

            return order;
        }

        async Task<IOrder> IOrdersProvider.GetOrderAsync(string orderId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ICollection<IOrder> GetOrders(ICollection<string> orderIds)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IOrder>> GetOrdersAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<ICollection<IOrder>> GetOrdersAsync(ICollection<string> orderIds, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Alters order by adding tpkd entries from the Choice/Month which have not been claimed to the source order to the current order as tkpd virtual entries
        /// </summary>
        /// <param name="order"></param>
        /// <param name="cancellationToken"></param>
        public async Task AddChoiceMonthlyGamesAsync(Order order, CancellationToken cancellationToken = default)
        {
            /*var result = await GetChoiceMonthDataAsync(order, cancellationToken);
            if (result.dbHit)
            {
                // DB already contains missing monthly entries; just append them and return
                if (result.extras?.Any() == true && order.tpkd_dict.all_tpks is List<ITpk> list)
                {
                    list.AddRange(result.extras);
                }
                return;
            }*/

            /*
            if (result.choiceMonth == null) return;

            // Merge from Choice/Monthly JSON
            MergeMonthlyGames(order, result.choiceMonth);*/

            // Persist newly added virtual TPKs so future runs can get them from the DB without
            // hitting the cache/network.
            
            foreach (var tpk in order.tpkd_dict.all_tpks.Where(t => t.is_virtual))
            {
                // Ensure machineName -> order linkage exists
                if (string.IsNullOrEmpty(tpk.gamekey)) tpk.gamekey = order.gamekey;
                orderRepository.Update(tpk);
            }
        }

        public async Task<(bool dbHit, List<ITpk> extras, IChoiceMonth choiceMonth)> GetChoiceMonthDataAsync(IOrder order, CancellationToken cancellationToken)
        {
            string versionCachePath;
            /*if (order.product.is_subs_v2_product)
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

            // 1) DB first: if DB already has additional tpks not present in current order, return them
            
            //var dbOrder = await orderRepository.GetOrderAsync(order.gamekey, cancellationToken);
            if (order?.tpkd_dict?.all_tpks != null)
            {
                var currentMachineNames = new HashSet<string>(order.tpkd_dict.all_tpks.Select(t => t.machine_name));
                var extras = order.tpkd_dict.all_tpks.Where(t => !currentMachineNames.Contains(t.machine_name)).ToList();
                if (extras.Any())
                {
                    return (true, extras, null);
                }
            }
            */

            var strChoiceMonth = string.Empty;
            var choiceUrl = $"https://www.humblebundle.com/membership/{order.product.choice_url}";
            /*
            // 2) File cache
            var fileCacheRepository = new FileCacheProvider(clientSettings, logger);
            var cachePath = $"membership/{versionCachePath}/{order.product.choice_url}";
            if (DateTime.TryParse(order.product.choice_url, out var choiceDate))
            {
                cachePath = $"membership/{versionCachePath}/{choiceDate:yyyy-MM}";
            }
            var orderCacheFilename = $"{fileCacheRepository.LocalCachePath}/{cachePath}.json";

            var cacheHit = false;
            if (preferCache)
            {
                strChoiceMonth = await fileCacheRepository.GetCacheContentAsync(orderCacheFilename, cancellationToken);
                cacheHit = !string.IsNullOrEmpty(strChoiceMonth);
            }
            */

            // 3) Live fetch if cache miss
            if (string.IsNullOrEmpty(strChoiceMonth))
            {
                webView.NavigateAndWait(choiceUrl);
                var match = Regex.Match(await webView.GetPageSourceAsync(),
                    @"<script id=""webpack-monthly-product-data"" type=""application/json"">([\s\S]*?)</script>");
                if (match.Success)
                {
                    strChoiceMonth = match.Groups[1].Value;
                }
                else
                {
                    logger.Error($"Unable to obtain Choice Monthly data for entry [{order.product.choice_url}]");
                }
            }

            if (string.IsNullOrEmpty(strChoiceMonth)) return (false, null, null);

            IChoiceMonth choiceMonth = null;
            if (order.product.is_subs_v2_product)
            {
                try
                {
                    choiceMonth = Serialization.FromJson<ChoiceMonthV2>(strChoiceMonth);
                }
                catch (Exception e)
                {
                    logger.Error(e, e.Message);
                    throw;
                }
                //logger.Trace($"Request:{choiceUrl} {(cacheHit?"From Cache ":"")}Content:{Serialization.ToJson(choiceMonth, true)}");
            }
            else if (order.product.is_subs_v3_product)
            {
                try
                {
                    choiceMonth = Serialization.FromJson<ChoiceMonthV3>(strChoiceMonth);
                }
                catch (Exception e)
                {
                    logger.Error(e, e.Message);
                    throw;
                }
                //logger.Trace($"Request:{choiceUrl} {(cacheHit?"From Cache ":"")}Content:{Serialization.ToJson(choiceMonth, true)}");
            }
            else
            {
                logger.Error("Unknown Choice Monthly product version");
            }

            return (false, null, choiceMonth);
        }
        
        public void MergeMonthlyGames(IOrder order, IChoiceMonth choiceMonth)
        {
            // Add contentChoice to all_tpks if it doesn't already exist (all_tpks gets populated by the order if it is already redeemed)
            // Only add to the order if the month contains redeemable games, may already have exhausted the selection count
            var orderMachineNames = order.tpkd_dict.all_tpks.Select(tpk => tpk.machine_name).ToList();

            var contentChoicesNotInOrder = choiceMonth.ContentChoices.Keys.ToList().Where(contentChoiceKey => !choiceMonth.ChoicesMade.Contains(contentChoiceKey));
            var choicesNotInOrder = contentChoicesNotInOrder.ToList();
            
            foreach (var contentChoiceKey in choicesNotInOrder)
            {
                // get tkpds either directly or via nested_choice_tpkds
                ICollection<ITpk> orderEntries = null; 
                var contentChoice = choiceMonth.ContentChoices[contentChoiceKey];
                if (contentChoice.tpkds != null)
                {
                    orderEntries = contentChoice.tpkds;
                }
                else if (contentChoice.nested_choice_tpkds != null)
                {
                    var nestedOrderEntries = new List<ITpk>();
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
                    contentChoiceTpkd.gamekey = order.gamekey;
                }

                if (order.tpkd_dict.all_tpks is List<ITpk> list)
                {
                    // Avoid duplicates by machine_name
                    var toAdd = orderEntries.Where(e => !orderMachineNames.Contains(e.machine_name)).ToList();
                    if (toAdd.Any())
                    {
                        list.AddRange(toAdd);
                        orderMachineNames.AddRange(toAdd.Select(e => e.machine_name));
                    }
                }
            }

            // All monthly keys claimed
            var unredeemedKeys = (from tpkd in order.tpkd_dict.all_tpks where tpkd.key_type == "steam" && tpkd.redeemed_key_val == null && !tpkd.is_expired select tpkd.machine_name).ToList();

            if (!unredeemedKeys.Any())
            {
                logger.Info($"All keys redeemed for bundle: {order.product.human_name}");
            }
            
            /*
            foreach (var tpk in order.tpkd_dict.all_tpks)
            {
                GameKeyRepository.Update(tpk);
            }*/
        }

        IOrder IOrdersProvider.GetOrder(string orderId)
        {
            return GetOrder(orderId);
        }

        public async Task<Order> GetOrderAsync(string key, bool retrieveLinkedRecords = false, CancellationToken cancellationToken = default)
        {
            var orderUri = string.Format(orderUrlMask, key);
            Order order = null;

            logger.Trace($"Fetching order details");
            webView.NavigateAndWait(orderUri);
            var strContent = await webView.GetPageTextAsync();

            order = Serialization.FromJson<Order>(strContent);
            if (retrieveLinkedRecords && order.IsChoiceOrder())
            {
                var humbleChoiceAsync = await orderRepository.GetHumbleChoiceAsync(order, cancellationToken);
                await AddChoiceMonthlyGamesAsync(order, cancellationToken);
            }
            logger.Trace($"Request:{orderUri} Content:{Serialization.ToJson(order, true)}");
            return order;
        }

        public void Dispose()
        {
            orderRepository?.Dispose();
            webView?.Dispose();
        }
    }
}
