using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Security.Authentication;
using System.Windows;
using System.Windows.Controls;
using HumbleKeys.Extensions;
using HumbleKeys.Models;
using HumbleKeys.Services;
using Order = HumbleKeys.Services.GameKey.Models.Order;

namespace HumbleKeys
{
    [LoadPlugin]
    public class HumbleKeysLibrary : LibraryPlugin
    {
        #region === Constants ================

        private static readonly ILogger logger = LogManager.GetLogger();
        private const string dbImportMessageId = "humblekeyslibImportError";
        private const string humblePurchaseUrlMask = @"https://www.humblebundle.com/downloads?key={0}";
        private const string steamGameUrlMask = @"https://store.steampowered.com/app/{0}";
        private const string steamSearchUrlMask = @"https://store.steampowered.com/search/?term={0}";
        internal const string REDEEMED_STR = "Key: Redeemed";
        internal const string UNREDEEMED_STR = "Key: Unredeemed";
        internal const string UNREDEEMABLE_STR = "Key: Unredeemable";
        internal const string EXPIRABLE_STR = "Key: Expirable";
        internal const string CLAIMED_STR = "Key: Claimed";
        internal const string UNCLAIMED_STR = "Key: Unclaimed";

        internal static readonly string[] PAST_TAGS =
            { REDEEMED_STR, UNREDEEMED_STR, UNREDEEMABLE_STR, EXPIRABLE_STR, CLAIMED_STR, UNCLAIMED_STR,
              "Redeemed", "Unredeemed", "Unredeemable", "Expirable", "Claimed", "Unclaimed",
              "Key: Expired", "Expired" };

        private const string HUMBLE_KEYS_SRC_NAME = "Humble Keys";
        private const string HUMBLE_KEYS_PLATFORM_NAME = "Humble Key: ";
        private const string NINTENDO_SWITCH = "nintendo_switch";
        private const string PC_WINDOWS = "pc_windows";

        #endregion

        #region === Variables ================

        private Platform winPlatform;
        private Platform switchPlatform;
        private readonly KeyInfo humbleKeysSource = new KeyInfo { Name = "Unknown" };
        private SidebarItem importProgress;
        private IHumbleOrderRepository _humbleOrderRepository;
        public override string Name => "Humble Keys";

        private IHumbleOrderRepository HumbleOrderRepository
        {
            get
            {
                if (_humbleOrderRepository != null) return _humbleOrderRepository;

                var webView = PlayniteApi.WebViews.CreateOffscreenView(new WebViewSettings { JavaScriptEnabled = false });

                _humbleOrderRepository = HumbleOrderRepositoryFactory.Create(webView, Settings, logger);
                return _humbleOrderRepository;
            }
        }

        #endregion
        
        #region === Accessors ================

        private Guid STEAMPLUGINID { get; } = Guid.Parse("cb91dfc9-b977-43bf-8e70-55f46e410fab");
        private HumbleKeysLibrarySettings Settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("62ac4052-e08a-4a1a-b70a-c2c0c3673bb9");

        // Implementing Client adds ability to open it via special menu in Playnite.
        public override LibraryClient Client { get; } = new HumbleKeysLibraryClient();

        #endregion

        public static event EventHandler UpdateProgress;

        public HumbleKeysLibrary(IPlayniteAPI api) : base(api)
        {
            Properties = new LibraryPluginProperties { CanShutdownClient = false, HasCustomizedGameImport = true, HasSettings = true };
            var settings = new HumbleKeysLibrarySettings(this);
            Settings = settings;
            EnsureLocalizationLoaded();
        }

        public HumbleKeysLibrary(IPlayniteAPI api, HumbleKeysLibrarySettings settings = null) : base(api)
        {
            Properties = new LibraryPluginProperties { CanShutdownClient = false, HasCustomizedGameImport = true, HasSettings = (settings != null) };
            Settings = settings;
            EnsureLocalizationLoaded();
        }

        private void EnsureLocalizationLoaded()
        {
            try
            {
                if (Application.Current != null && !Application.Current.Resources.Contains("LOCHumbleKeysCopyKeyMenuItem"))
                {
                    var pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
                    var locPath = Path.Combine(pluginDir, "Localization", "en_US.xaml");
                    if (File.Exists(locPath))
                    {
                        using (var stream = File.OpenRead(locPath))
                        {
                            var dict = (ResourceDictionary)System.Windows.Markup.XamlReader.Load(stream);
                            Application.Current.Resources.MergedDictionaries.Add(dict);
                            return;
                        }
                    }

                    var packUri = new Uri("pack://application:,,,/HumbleKeysLibrary;component/Localization/en_US.xaml", UriKind.Absolute);
                    var embeddedDict = new ResourceDictionary { Source = packUri };
                    Application.Current.Resources.MergedDictionaries.Add(embeddedDict);
                }
            }
            catch (Exception ex)
            {
                logger?.Warn($"Failed to load localization fallback: {ex.Message}");
            }
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return Settings as ISettings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new HumbleKeysLibrarySettingsView();
        }

        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            importProgress = new SidebarItem
            {
                ProgressMaximum = 100f,
                ProgressValue = 0f,
                Type = SiderbarItemType.Button,
                Icon = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "icon.png"),
                Title = "Humble Remote Progress",
                Visible = false
            };
            yield return importProgress;
        }

        public override IEnumerable<Game> ImportGames(LibraryImportGamesArgs args)
        {

            var importedGames = new List<Game>();
            var removedGames = new List<Game>();
            Exception importError = null;

            if (!Settings.ConnectAccount)
            {
                return importedGames;
            }

            try
            {
                if (!args.CancelToken.IsCancellationRequested)
                {
                    var ordersAsync = ScrapeOrdersAsync(args.CancelToken);
                    if (!ordersAsync.IsCanceled)
                    {
                        ordersAsync.Wait(args.CancelToken);
                        var orders = ordersAsync.Result;
                        // create dictionary indexed by gamekey
                        var indexedOrders = orders.ToDictionary(order => order.gamekey, order => order);
                        var selectedTpkds = SelectTpkds(indexedOrders);
                        var tpkdsList = selectedTpkds.ToList();
                        logger.Trace("ImportGames: Selected Tpkds Count = " + tpkdsList.Count);
                        var processOrdersAsync = ProcessOrdersAsync(indexedOrders, tpkdsList, importedGames, removedGames, args.CancelToken);
                        processOrdersAsync.Wait(args.CancelToken);
                    }
                }
            }
            catch (OperationCanceledException operationCanceledException)
            {
                logger.Trace("Operation Cancelled by user");
            }
            catch (Exception e)
            {
                if (e.InnerException is AuthenticationException)
                {
                    importError = e.InnerException;
                }
                else
                {
                    importError = e;
                    logger.Error($"Humble Keys Library: error {e}");
                }
            }

            if (importError != null)
            {
                logger.Error($"Humble Keys Library: importError {dbImportMessageId}");
                PlayniteApi.Notifications.Add(new NotificationMessage(
                    dbImportMessageId,
                    string.Format(ResourceProvider.GetString("LOCLibraryImportError"), Name) +
                    Environment.NewLine + importError.Message,
                    NotificationType.Error,
                    () => OpenSettingsView()));
            }
            else
            {
                PlayniteApi.Notifications.Remove(dbImportMessageId);
            }

            logger.Trace($"ImportGames: Imported {importedGames.Count} games, Removed {removedGames.Count} games");
            // Resets instance so factory can be reinitialised later

            _humbleOrderRepository = null;
            return importedGames;
        }

        private async Task<IEnumerable<string>> GetOrderKeysAsync(CancellationToken cancellationToken = default)
        {
            return await HumbleOrderRepository.GetLibraryKeysAsync(cancellationToken);
        }

        private async Task<IOrder> GetOrderAsync(string gameKey, CancellationToken cancellationToken = default)
        {
            return await HumbleOrderRepository.GetOrderAsync(gameKey, cancellationToken: cancellationToken);
            //return await OrderHandler.ExecuteAsync(gameKey, cancellationToken);
        }
        
        public async Task<ICollection<IOrder>> ScrapeOrdersAsync(CancellationToken cancellationToken = default)
        {
            var orders = new Collection<IOrder>();
            // use chain of responsibility to get library keys
            var orderKeys = await GetOrderKeysAsync(cancellationToken);
            var orderKeysList = orderKeys?.ToList() ?? new List<string>();
            
            if (importProgress != null)
            {
                importProgress.ProgressValue = 0f;
                importProgress.Visible = true;
            }

            var stopwatch = new Stopwatch();
            for (var i = 0; i < orderKeysList.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;
                    
                var gameKey = orderKeysList[i];
                try
                {
                    if (importProgress != null) importProgress.ProgressValue = ((float)i / orderKeysList.Count) * 100;

                    stopwatch.Reset();
                    stopwatch.Start();
                    var order = await GetOrderAsync(gameKey, cancellationToken);
                    stopwatch.Stop();
                    logger?.Trace($"ScrapeOrdersAsync::GetOrderAsync({gameKey}) completed in {stopwatch.Elapsed.ToString()} ms");    
                    orders.Add(order);
                }
                catch (Exception e)
                {
                    logger?.Error(e, e.Message);
                }
            }

            if (importProgress != null)
            {
                importProgress.Visible = false;
                importProgress.ProgressValue = 100f;
            }

            logger.Trace("ScrapeOrders: Orders Count = " + orders.Count);

            return orders;
        }

        public IEnumerable<IGrouping<string, ITpk>> SelectTpkds(Dictionary<string, IOrder> orders)
        {
            return orders.Select(kv =>
                    kv.Value)
                .SelectMany(a =>
                    a.tpkd_dict?.all_tpks)
                .Where(t =>
                    t != null
                    && Settings.keyTypeWhitelist.ContainsKey(t.key_type)
                    && !string.IsNullOrWhiteSpace(t.gamekey)
                ).GroupBy(tpk =>
                    tpk.gamekey);
        }

        /// <summary>
        /// Adds all keys from @tpkds
        /// an Order may be a single purchase, a bundle purchase or a monthly subscription
        /// </summary>
        /// <param name="orders"></param>
        /// <param name="tpkds"></param>
        /// <param name="importedGames">List of Games added from orders</param>
        /// <param name="removedGames">List of Games removed from orders due to settings</param>
        protected async Task ProcessOrdersAsync(Dictionary<string, IOrder> orders, IEnumerable<IGrouping<string, ITpk>> tpkds, List<Game> importedGames, List<Game> removedGames, CancellationToken cancellationToken = default)
        {
            var redeemedTag = PlayniteApi.Database.Tags.Add(REDEEMED_STR);
            var unredeemedTag = PlayniteApi.Database.Tags.Add(UNREDEEMED_STR);
            var unredeemableTag = PlayniteApi.Database.Tags.Add(UNREDEEMABLE_STR);
            var claimedTag = PlayniteApi.Database.Tags.Add(CLAIMED_STR);
            var unclaimedTag = PlayniteApi.Database.Tags.Add(UNCLAIMED_STR);

            var tagMethod = (TagMethodology)Settings.TagWithBundleName;
            var unredeemableMethod = (UnredeemableMethodology)Settings.UnredeemableKeyHandling;

            if (winPlatform == null)
                winPlatform =
                    PlayniteApi.Database.Platforms.FirstOrDefault(platform => platform.SpecificationId == PC_WINDOWS);
            if (switchPlatform == null)
                switchPlatform =
                    PlayniteApi.Database.Platforms.FirstOrDefault(platform =>
                        platform.SpecificationId == NINTENDO_SWITCH);

            logger.Trace("ProcessOrders: DB begin update");
            var steamLibraryPlugin = PlayniteApi.Addons.Plugins.FirstOrDefault(plugin => plugin.Id == STEAMPLUGINID);
            var steamGameIds = steamLibraryPlugin != null
                ? new HashSet<string>(
                    PlayniteApi.Database.Games
                        .Where(game => game.PluginId == steamLibraryPlugin.Id && !string.IsNullOrEmpty(game.GameId))
                        .Select(game => game.GameId))
                : new HashSet<string>();
            var steamGameNames = steamLibraryPlugin != null
                ? new HashSet<string>(
                    PlayniteApi.Database.Games
                        .Where(game => game.PluginId == steamLibraryPlugin.Id && !string.IsNullOrEmpty(game.Name))
                        .Select(game => game.Name),
                    StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            bool ExistsInSteam(ITpk tpk)
            {
                if (tpk == null || steamLibraryPlugin == null) return false;
                var isNonSteam = !string.IsNullOrEmpty(tpk.key_type) && tpk.key_type != "steam" && string.IsNullOrEmpty(tpk.steam_app_id);
                if (isNonSteam) return false;

                if (!string.IsNullOrEmpty(tpk.steam_app_id))
                {
                    return steamGameIds.Contains(tpk.steam_app_id);
                }

                if (!string.IsNullOrEmpty(tpk.human_name))
                {
                    return steamGameNames.Contains(tpk.human_name);
                }

                return false;
            }

            PlayniteApi.Database.BeginBufferUpdate();
            try
            {
                foreach (var tpkdGroup in tpkds)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    var tpkdGroupEntries = tpkdGroup.AsEnumerable();
                    Tag humbleChoiceTag = null;
                    var groupEntries = tpkdGroupEntries.ToList();
                    if (Settings.ImportChoiceKeys && tagMethod != TagMethodology.None && groupEntries.Count() > 1)
                    {
                        var isHumbleMonthly = orders[tpkdGroup.Key].product.human_name.Contains("Humble Monthly");
                        if (tagMethod == TagMethodology.All || tagMethod == TagMethodology.Monthly && isHumbleMonthly)
                        {
                            humbleChoiceTag =
                                PlayniteApi.Database.Tags.Add($"Bundle: {orders[tpkdGroup.Key].product.human_name}");
                        }
                    }

                    var bundleContainsUnredeemableKeys = false;
                    var sourceOrder = orders[tpkdGroup.Key];
                    if (sourceOrder != null && sourceOrder.product.category != "storefront" &&
                        sourceOrder.total_choices > 0 && sourceOrder.product.is_subs_v2_product)
                    {
                        bundleContainsUnredeemableKeys = sourceOrder.choices_remaining == 0;
                    }

                    // Monthly bundle has all choices made
                    if (bundleContainsUnredeemableKeys && humbleChoiceTag != null)
                    {
                        // search Playnite db for all games that are not included in groupEntries, these can be removed
                        var virtualOrders = groupEntries.Where(tpk => tpk.is_virtual).Select(GetGameId) ??
                                            new List<string>();
                        var gameKeys = virtualOrders.ToList();
                        // for this bundle, get all games from the database that are not in the keys collection for this order
                        var libraryKeysNotInOrder = PlayniteApi.Database.Games
                            .Where(game =>
                                game.TagIds != null && game.TagIds.Contains(humbleChoiceTag.Id) &&
                                gameKeys.Contains(game.GameId))
                            .ToList();
                        foreach (var game in libraryKeysNotInOrder)
                        {
                            var matchingTpk = groupEntries.FirstOrDefault(tpk => GetGameId(tpk) == game.GameId);
                            var inSteam = matchingTpk != null ? ExistsInSteam(matchingTpk) : (!string.IsNullOrEmpty(game.Name) && steamGameNames.Contains(game.Name));
                            if (inSteam)
                            {
                                EnsureTagList(game);
                                game.TagIds.Remove(unclaimedTag.Id);
                                game.TagIds.Remove(unredeemedTag.Id);
                                var changed = false;
                                if (!game.TagIds.Contains(redeemedTag.Id)) { game.TagIds.Add(redeemedTag.Id); changed = true; }
                                if (!game.TagIds.Contains(unredeemableTag.Id)) { game.TagIds.Add(unredeemableTag.Id); changed = true; }
                                if (changed) PlayniteApi.Database.Games.Update(game);
                                continue;
                            }

                            switch (unredeemableMethod)
                            {
                                case UnredeemableMethodology.Tag:
                                {
                                    EnsureTagList(game);
                                    if (game.TagIds.Contains(unredeemableTag.Id)) continue;

                                    game.TagIds.Add(unredeemableTag.Id);
                                    PlayniteApi.Notifications.Add(
                                        new NotificationMessage("HumbleKeysLibraryUpdate_" + game.Id,
                                            $"{game.Name} is no longer redeemable", NotificationType.Info,
                                            () =>
                                            {
                                                if (PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen)
                                                    return;
                                                PlayniteApi.MainView.SelectGame(game.Id);
                                            })
                                    );
                                    PlayniteApi.Database.Games.Update(game);
                                    break;
                                }
                                case UnredeemableMethodology.Delete:
                                {
                                    if (PlayniteApi.Database.Games.Remove(game))
                                    {
                                        removedGames.Add(game);
                                    }

                                    break;
                                }
                            }
                        }
                    }

                    foreach (var tpkd in groupEntries)
                    {
                        var existsInSteam = ExistsInSteam(tpkd);
                        var gameId = GetGameId(tpkd);

                        var gameEntry =
                            PlayniteApi.Database.Games.FirstOrDefault(game =>
                                game.GameId == gameId && game.PluginId == Id);

                        var newGameEntry = false;
                        if (gameEntry == null)
                        {
                            if (!Settings.IgnoreRedeemedKeys || (Settings.IgnoreRedeemedKeys && !GetOrderRedemptionTags(tpkd, existsInSteam).Contains(REDEEMED_STR)))
                            {
                                gameEntry = ImportNewGame(tpkd, humbleChoiceTag, sourceOrder, existsInSteam);
                                importedGames.Add(gameEntry);
                                newGameEntry = true;
                            }
                        }

                        if (gameEntry != null)
                        {
                            if (Settings.ExpirableNotification)
                            {
                                if (GetOrderRedemptionTags(tpkd, existsInSteam).Contains(EXPIRABLE_STR))
                                {
                                    PlayniteApi.Notifications.Add(
                                        new NotificationMessage("HumbleKeysLibraryUpdate_expirable_" + gameEntry.Name,
                                            $"{gameEntry.Name}: Has an expiration date, it will expire in {tpkd.num_days_until_expired} days",
                                            NotificationType.Info,
                                            () =>
                                            {
                                                if (PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen) return;
                                                PlayniteApi.MainView.SelectGame(gameEntry.Id);
                                            })
                                    );
                                    var expiryNote = tpkd.expiration_date != DateTime.MinValue
                                        ? $"Key expires on: {tpkd.expiration_date.ToString(CultureInfo.CurrentCulture)}\n"
                                        : $"Key expires on: {DateTime.Now.AddDays(tpkd.num_days_until_expired).ToString(CultureInfo.CurrentCulture)}\n";
                                    if (string.IsNullOrEmpty(gameEntry.Notes))
                                    {
                                        gameEntry.Notes = expiryNote;
                                        PlayniteApi.Database.Games.Update(gameEntry);
                                    }
                                    else if (!gameEntry.Notes.Contains(expiryNote))
                                    {
                                        gameEntry.Notes += expiryNote;
                                        PlayniteApi.Database.Games.Update(gameEntry);
                                    }
                                }
                            }

                            if (Settings.UnclaimedGameNotification)
                            {
                                // key present but no matching game in steam library
                                if (tpkd.steam_app_id != null)
                                {
                                    if (!existsInSteam && GetOrderRedemptionTagState(tpkd, existsInSteam) == CLAIMED_STR)
                                    {
                                        PlayniteApi.Notifications.Add(
                                            new NotificationMessage("HumbleKeysLibraryUpdate_unclaimed_game_" + gameEntry.Id,
                                                $"{gameEntry.Name} does not exist in Steam library",
                                                NotificationType.Info,
                                                () =>
                                                {
                                                    if (PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen)
                                                        return;
                                                    PlayniteApi.MainView.SelectGame(gameEntry.Id);
                                                })
                                        );
                                    }
                                }
                            }

                            if (tpkd.sold_out && !IsKeyPresent(tpkd))
                            {
                                PlayniteApi.Notifications.Add(
                                    new NotificationMessage("HumbleKeysLibraryUpdate_sold_out_" + gameEntry.Name,
                                        $"{gameEntry.Name} Key has sold out",
                                        NotificationType.Info,
                                        () =>
                                        {
                                            if (PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen) return;
                                            PlayniteApi.MainView.SelectGame(gameEntry.Id);
                                        })
                                );
                            }
                        }

                        if (newGameEntry)
                        {
                            continue;
                        }

                        if (gameEntry != null)
                        {
                            if (!Settings.IgnoreRedeemedKeys || (Settings.IgnoreRedeemedKeys && !GetOrderRedemptionTags(tpkd, existsInSteam).Contains(REDEEMED_STR)))
                            {
                                var tagsUpdated = UpdateRedemptionStatus(gameEntry, tpkd, humbleChoiceTag, existsInSteam);
                                var otherUpdated = UpdatePlatform(gameEntry, tpkd);
                                if (UpdateRedemptionStore(gameEntry, tpkd)) otherUpdated = true;
                                if (UpdateMetaData(gameEntry, sourceOrder, tpkd, humbleChoiceTag)) otherUpdated = true;

                                if (Settings.AddLinks)
                                {
                                    if (gameEntry.Links == null)
                                    {
                                        gameEntry.Links = new ObservableCollection<Link>();
                                    }

                                    if (UpdateStoreLinks(gameEntry.Links, tpkd, true)) otherUpdated = true;
                                }

                                if (!tagsUpdated && !otherUpdated)
                                {
                                    logger.Trace(
                                        $"ProcessOrders: No update needed for '{gameEntry.Name}' with GameId = {gameEntry.GameId}");
                                    continue;
                                }

                                if (gameEntry.TagIds != null &&
                                    gameEntry.TagIds.Contains(unredeemableTag.Id) &&
                                    !gameEntry.TagIds.Contains(redeemedTag.Id))
                                {
                                    switch (unredeemableMethod)
                                    {
                                        case UnredeemableMethodology.Tag:
                                        {
                                            PlayniteApi.Database.Games.Update(gameEntry);
                                            PlayniteApi.Notifications.Add(
                                                new NotificationMessage("HumbleKeysLibraryUpdate_" + gameEntry.Id,
                                                    $"{gameEntry.Name} is no longer redeemable",
                                                    NotificationType.Info,
                                                    () =>
                                                    {
                                                        if (PlayniteApi.ApplicationInfo.Mode ==
                                                            ApplicationMode.Fullscreen)
                                                            return;
                                                        PlayniteApi.MainView.SelectGame(gameEntry.Id);
                                                    })
                                            );
                                            break;
                                        }
                                        case UnredeemableMethodology.Delete:
                                        {
                                            if (PlayniteApi.Database.Games.Remove(gameEntry))
                                            {
                                                removedGames.Add(gameEntry);
                                            }

                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    PlayniteApi.Database.Games.Update(gameEntry);
                                    logger.Trace(
                                        $"ProcessOrders: Updated '{gameEntry.Name}' with GameId = {gameEntry.GameId}");
                                    if (tagsUpdated)
                                    {
                                        PlayniteApi.Notifications.Add(
                                            new NotificationMessage("HumbleKeysLibraryUpdate_" + gameEntry.Id,
                                                $"Tags Updated for {gameEntry.Name}: " +
                                                GetOrderRedemptionTagState(tpkd, existsInSteam), NotificationType.Info,
                                                () =>
                                                {
                                                    if (PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen)
                                                        return;
                                                    PlayniteApi.MainView.SelectGame(gameEntry.Id);
                                                })
                                        );
                                    }
                                }
                            }
                            else
                            {
                                // Remove Existing Game?
                                PlayniteApi.Database.Games.Remove(gameEntry);
                                logger.Trace(
                                    $"Removing game '{gameEntry.Name}' with GameId = {gameEntry.GameId} since Settings.IgnoreRedeemedKeys is: [{Settings.IgnoreRedeemedKeys}] and state is [{GetOrderRedemptionTagState(tpkd, existsInSteam)}]");
                            }
                        }
                    }
                }
            }
            finally
            {
                PlayniteApi.Database.EndBufferUpdate();
                logger.Trace("ProcessOrders: DB update complete");
            }
        }

        bool UpdateStoreLinks(ObservableCollection<Link> links, ITpk tpkd, bool useDispatcher)
        {
            var recordChanged = false;

            // add link to Humble purchase
            if (!string.IsNullOrWhiteSpace(tpkd?.gamekey))
            {
                var humbleLink = MakeLink(tpkd?.gamekey);

                if (!links.Contains(humbleLink))
                {
                    if (useDispatcher)
                    {
                        API.Instance.MainView.UIDispatcher.Invoke(delegate { links.Add(humbleLink); });
                    }
                    else
                    {
                        links.Add(humbleLink);
                    }

                    recordChanged = true;
                }
            }

            if (tpkd?.key_type != "steam") return recordChanged;

            var steamGameLink = tpkd.MakeSteamLink();
            if (steamGameLink == null) return false;
            
            var steamLinks = links.Where((link1, i) => link1.Name == "Steam");
            var steamLinksList = steamLinks.ToList();
            var existingSteamLink = steamLinksList.FirstOrDefault();
            if (existingSteamLink == null)
            {
                if (useDispatcher)
                {
                    API.Instance.MainView.UIDispatcher.Invoke(delegate { links.Add(steamGameLink); });
                }
                else
                {
                    links.Add(steamGameLink);
                }

                return true;
            }

            if (!string.IsNullOrEmpty(tpkd.steam_app_id) && existingSteamLink.Url == steamGameLink.Url)
                return recordChanged;

            // steam link url doesn't match expected value
            if (existingSteamLink.Url != steamGameLink.Url)
            {
                existingSteamLink.Url = steamGameLink.Url;
            }
            else
            {
                return recordChanged;
            }

            return true;
        }

        Game ImportNewGame(ITpk tpkd, Tag groupTag = null, IOrder sourceOrder = null, bool existsInSteamLibrary = true)
        {
            var gameInfo = new GameMetadata()
            {
                Name = tpkd.human_name,
                GameId = GetGameId(tpkd),
                Source = new MetadataNameProperty(HUMBLE_KEYS_SRC_NAME),
                Platforms = new HashSet<MetadataProperty>
                {
                    new MetadataNameProperty(
                        HUMBLE_KEYS_PLATFORM_NAME + tpkd.key_type)
                },
                Tags = new HashSet<MetadataProperty>(),
                Links = new List<Link>(),
                
            };

            if (Settings.RedemptionStore != (int)RedemptionStoreType.Source)
            {
                gameInfo.Source = new MetadataNameProperty(HUMBLE_KEYS_SRC_NAME);
            }

            if (Settings.AddKeyStatus)
            {
                // add tag(s) reflecting redemption status
                var tags = GetOrderRedemptionTags(tpkd, existsInSteamLibrary);
                if (gameInfo.Tags == null)
                {
                    gameInfo.Tags = new HashSet<MetadataProperty>();
                }

                foreach (var tagName in tags)
                {
                    gameInfo.Tags.Add(new MetadataNameProperty(tagName));
                }
            }

            if (Settings.AddLinks)
            {
                var links = new ObservableCollection<Link>();
                if (UpdateStoreLinks(links, tpkd, false))
                {
                    gameInfo.Links = new List<Link>();
                    gameInfo.Links.AddRange(links.ToList());
                }
            }

            // no need to call BeginBufferUpdate() here because the only place this method is called already did that
            var game = PlayniteApi.Database.ImportGame(gameInfo, this);
            var gameChanged = false;

            if (groupTag != null)
            {
                EnsureTagList(game);
                game.TagIds.Add(groupTag.Id);
                gameChanged = true;
            }

            if (UpdatePlatform(game, tpkd)) gameChanged = true;
            if (UpdateRedemptionStore(game, tpkd)) gameChanged = true;
            if (sourceOrder != null && UpdateMetaData(game, sourceOrder, tpkd, groupTag)) gameChanged = true;

            if (gameChanged)
            {
                PlayniteApi.Database.Games.Update(game);
            }

            logger.Trace($"ImportNewGame: Added '{game.Name}' with GameId = {game.GameId}");
            return game;
        }

        internal static bool UpdateMetaData(Game alreadyImported, IOrder sourceOrder, ITpk tpkd = null,
            Tag humbleChoiceTag = null)
        {
            if (alreadyImported == null || sourceOrder == null || sourceOrder.created == DateTime.MinValue)
                return false;

            var createdUtcDateTime = sourceOrder.created.ToUniversalTime();
            if (alreadyImported.Added != null)
            {
                var addedUtcDateTime = alreadyImported.Added.Value.Kind != DateTimeKind.Utc
                    ? alreadyImported.Added.Value.ToUniversalTime()
                    : alreadyImported.Added.Value;
                var timeDifference = addedUtcDateTime - createdUtcDateTime;
                if (timeDifference.Days == 0 && timeDifference.Hours == 0 && timeDifference.Minutes == 0 &&
                    timeDifference.Seconds == 0)
                    return false;
            }

            alreadyImported.Added = createdUtcDateTime.ToLocalTime();
            return true;
        }

        // If a game is expired, add tag 'Key: Unredeemable'
        // If a game had been redeemed since last added to Playnite, remove the tag 'Key: Unredeemed' and add the tag 'Key: Redeemed'
        // returns whether tags were updated or not
        bool UpdateRedemptionStatus(Game existingGame, ITpk tpkd, Tag groupTag = null, bool existsInSteamLibrary = true)
        {
            var recordChanged = false;
            if (existingGame == null)
            {
                return false;
            }

            if (!Settings.keyTypeWhitelist.ContainsKey(tpkd.key_type))
            {
                return false;
            }

            if (groupTag != null)
            {
                if (existingGame.Tags == null || existingGame.Tags.All(tag => tag.Id != groupTag.Id))
                {
                    EnsureTagList(existingGame);
                    existingGame.TagIds.Add(groupTag.Id);
                    recordChanged = true;
                }
            }

            if (!Settings.AddKeyStatus) return recordChanged;

            // process tags on existingGame only if there was a change in tag status
            var existingRedemptionTags =
                existingGame.Tags?.Where(t => PAST_TAGS.Contains(t.Name)).ToList() ??
                new List<Tag>();
            var existingRedemptionTagIds = new HashSet<Guid>(existingRedemptionTags.Select(tag => tag.Id));

            var expectedTagNames = GetOrderRedemptionTags(tpkd, existsInSteamLibrary);
            var expectedTagIds = new HashSet<Guid>(expectedTagNames.Select(name => PlayniteApi.Database.Tags.Add(name).Id));

            // existingGame already tagged with correct tag state
            if (existingRedemptionTagIds.SetEquals(expectedTagIds)) return recordChanged;

            EnsureTagList(existingGame);

            // remove all tags related to key state
            existingGame.TagIds.RemoveAll(tagId => existingRedemptionTagIds.Contains(tagId));

            // add all expected tags
            foreach (var expectedTagId in expectedTagIds)
            {
                if (!existingGame.TagIds.Contains(expectedTagId))
                {
                    existingGame.TagIds.Add(expectedTagId);
                }
            }

            return true;
        }

        // Add Platform if needed
        // returns whether it was updated or not 
        bool UpdatePlatform(Game game, ITpk tpkd)
        {
            var recordChanged = false;

            if (tpkd.key_type == "nintendo_direct")
            {
                // Add "Nintendo Switch" for all Nintendo keys
                if (Settings.AddPlatformNintendo)
                {
                    if (game.Platforms?.FirstOrDefault(platform => platform.SpecificationId == NINTENDO_SWITCH) == null)
                    {
                        EnsurePlatformList(game);
                        game.PlatformIds.Add(switchPlatform.Id);
                        recordChanged = true;
                    }
                }
            }
            else
            {
                // Add default "PC (Windows)" for all other keys
                if (Settings.AddPlatformWindows)
                {
                    if (game.Platforms?.FirstOrDefault(platform => platform.SpecificationId == PC_WINDOWS) == null)
                    {
                        EnsurePlatformList(game);
                        game.PlatformIds.Add(winPlatform.Id);
                        recordChanged = true;
                    }
                }
            }

            return recordChanged;
        }

        // Add Redemption Store if needed
        // returns whether it was updated or not
        bool UpdateRedemptionStore(Game game, ITpk tpkd)
        {
            if (Settings.RedemptionStore == (int)RedemptionStoreType.None) return false;
            var recordChanged = false;
            var newSource = GetKeyInfo(tpkd.key_type, Settings.RedemptionStore == (int)RedemptionStoreType.Source);
            string newName = HUMBLE_KEYS_PLATFORM_NAME + newSource.Name;

            switch (Settings.RedemptionStore)
            {
                case (int)RedemptionStoreType.Source:
                    if (game.SourceId != newSource.SourceId)
                    {
                        game.SourceId = newSource.SourceId;
                        recordChanged = true;
                    }

                    break;
                case (int)RedemptionStoreType.Tag:
                    var newTag = PlayniteApi.Database.Tags.FirstOrDefault(tag => tag.Name == newName) ??
                                 PlayniteApi.Database.Tags.Add(newName);
                    EnsureTagList(game);

                    if (!game.TagIds.Contains(newTag.Id))
                    {
                        game.TagIds.Add(newTag.Id);
                        recordChanged = true;
                    }

                    break;
                case (int)RedemptionStoreType.Category:
                    var newCat = PlayniteApi.Database.Categories.FirstOrDefault(category => category.Name == newName) ??
                                 PlayniteApi.Database.Categories.Add(newName);
                    EnsureCategoryList(game);

                    if (!game.CategoryIds.Contains(newCat.Id))
                    {
                        game.CategoryIds.Add(newCat.Id);
                        recordChanged = true;
                    }

                    break;
                case (int)RedemptionStoreType.Platform:
                    var newPlat = PlayniteApi.Database.Platforms.FirstOrDefault(platform => platform.Name == newName) ??
                                  PlayniteApi.Database.Platforms.Add(newName);

                    EnsurePlatformList(game);
                    if (!game.PlatformIds.Contains(newPlat.Id))
                    {
                        game.PlatformIds.Add(newPlat.Id);
                        recordChanged = true;
                    }

                    break;
            }

            return recordChanged;
        }

        KeyInfo GetKeyInfo(string key_type, bool needSourceId)
        {
            if (Settings.keyTypeWhitelist.TryGetValue(key_type, out KeyInfo keyInfo))
            {
                if (needSourceId && keyInfo.SourceId == Guid.Empty)
                {
                    var source = PlayniteApi.Database.Sources.FirstOrDefault(src => src.Name == keyInfo.SourceName) ??
                                 PlayniteApi.Database.Sources.Add(new MetadataNameProperty(keyInfo.SourceName));
                    keyInfo.SourceId = source.Id;
                }

                return keyInfo;
            }
            else
            {
                if (needSourceId && humbleKeysSource.SourceId == Guid.Empty)
                {
                    var source = PlayniteApi.Database.Sources.FirstOrDefault(src => src.Name == HUMBLE_KEYS_SRC_NAME) ??
                                 PlayniteApi.Database.Sources.Add(new MetadataNameProperty(HUMBLE_KEYS_SRC_NAME));
                    humbleKeysSource.SourceId = source.Id;
                }

                return humbleKeysSource;
            }
        }

        #region === Helper Methods ============

        private static string GetGameId(ITpk tpk) => $"{tpk.machine_name}_{tpk.gamekey}";

        private static Link MakeLink(string gameKey) =>
            new Link("Humble Purchase URL", string.Format(humblePurchaseUrlMask, gameKey));

        private static Link MakeSteamLink(string gameKey) =>
            new Link("Steam", string.Format(steamGameUrlMask, gameKey));

        private static bool IsKeyNull(ITpk t) => t?.redeemed_key_val == null;
        private static bool IsKeyPresent(ITpk t) => !IsKeyNull(t);

        internal static List<string> GetOrderRedemptionTags(ITpk t, bool? existsInSteamLibrary = null)
        {
            var isNonSteam = !string.IsNullOrEmpty(t.key_type) && t.key_type != "steam" && string.IsNullOrEmpty(t.steam_app_id);
            var inSteam = isNonSteam ? IsKeyPresent(t) : (existsInSteamLibrary == true);

            // 1. Determine Base State
            string baseTag;
            if (IsKeyPresent(t))
            {
                baseTag = inSteam ? REDEEMED_STR : UNREDEEMED_STR;
            }
            else
            {
                baseTag = t.is_virtual ? UNCLAIMED_STR : CLAIMED_STR;
            }

            var tags = new List<string> { baseTag };

            // 2. Determine Lifecycle Modifier
            var isPastExpiration = t.is_expired || (t.expiration_date != DateTime.MinValue && t.expiration_date < DateTime.Now);
            var isExpirable = (t.num_days_until_expired > 0) || (t.expiration_date != DateTime.MinValue && !isPastExpiration);

            if (isPastExpiration)
            {
                tags.Add(UNREDEEMABLE_STR);
            }
            else if (isExpirable)
            {
                tags.Add(EXPIRABLE_STR);
            }

            return tags;
        }

        internal static string GetOrderRedemptionTagState(ITpk t, bool? existsInSteamLibrary = null)
        {
            return string.Join(", ", GetOrderRedemptionTags(t, existsInSteamLibrary));
        }

        private static void EnsureTagList(Game game)
        {
            if (game.TagIds == null) game.TagIds = new List<Guid>();
        }

        private static void EnsurePlatformList(Game game)
        {
            if (game.PlatformIds == null) game.PlatformIds = new List<Guid>();
        }

        private static void EnsureCategoryList(Game game)
        {
            if (game.CategoryIds == null) game.CategoryIds = new List<Guid>();
        }

        #endregion

        private static void OnUpdateProgress(float  progress = 100f)
        {
            UpdateProgress?.Invoke(null, EventArgs.Empty);
        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            if (args.Games.Count != 1) yield break;
            var game = args.Games[0];
            if (game.PluginId != Id) yield break;

            var claimedTag = PlayniteApi.Database.Tags.FirstOrDefault(t => t.Name == CLAIMED_STR);
            var unclaimedTag = PlayniteApi.Database.Tags.FirstOrDefault(t => t.Name == UNCLAIMED_STR);
            var redeemedTag = PlayniteApi.Database.Tags.FirstOrDefault(t => t.Name == REDEEMED_STR);
            var unredeemedTag = PlayniteApi.Database.Tags.FirstOrDefault(t => t.Name == UNREDEEMED_STR);

            if (game.TagIds != null && (
                (claimedTag != null && game.TagIds.Contains(claimedTag.Id)) ||
                (unclaimedTag != null && game.TagIds.Contains(unclaimedTag.Id))) &&
                (redeemedTag == null || !game.TagIds.Contains(redeemedTag.Id))/* &&
                (unredeemedTag == null || !game.TagIds.Contains(unredeemedTag.Id))*/)
            {
                yield break;
            }

            var menuDesc = ResourceProvider.GetString("LOCHumbleKeysCopyKeyMenuItem");
            if (string.IsNullOrEmpty(menuDesc) || menuDesc == "LOCHumbleKeysCopyKeyMenuItem" || menuDesc.StartsWith("<LOC"))
            {
                menuDesc = "Copy CD Key to Clipboard";
            }

            yield return new GameMenuItem
            {
                Description = menuDesc,
                MenuSection = "Humble Keys",
                Action = _ => CopyKeyToClipboard(game)
            };
        }

        private void CopyKeyToClipboard(Game game)
        {
            try
            {
                var notInCacheMsg = ResourceProvider.GetString("LOCHumbleKeysCopyKeyNotInCache");
                if (string.IsNullOrEmpty(notInCacheMsg) || notInCacheMsg == "LOCHumbleKeysCopyKeyNotInCache" || notInCacheMsg.StartsWith("<LOC"))
                {
                    notInCacheMsg = "CD key not found in local cache. Re-sync your library to populate the cache.";
                }

                using var sqlRepo = new HumbleOrderSqlRepository(Settings, logger);
                var orderKeys = sqlRepo.GetLibraryKeys().ToList();
                var matchingKey = orderKeys.FirstOrDefault(k => game.GameId.EndsWith("_" + k));

                if (matchingKey == null)
                {
                    PlayniteApi.Dialogs.ShowMessage(notInCacheMsg, "Humble Keys");
                    return;
                }

                var machineName = game.GameId.Substring(0, game.GameId.Length - matchingKey.Length - 1);
                var order = sqlRepo.GetOrder(matchingKey);
                var tpk = order?.tpkd_dict?.all_tpks?.FirstOrDefault(t => t.machine_name == machineName);

                if (tpk?.redeemed_key_val == null)
                {
                    PlayniteApi.Dialogs.ShowMessage(notInCacheMsg, "Humble Keys");
                    return;
                }

                var key = tpk.redeemed_key_val is Newtonsoft.Json.Linq.JValue jVal
                    ? jVal.Value?.ToString()
                    : tpk.redeemed_key_val.ToString();

                if (string.IsNullOrWhiteSpace(key))
                {
                    PlayniteApi.Dialogs.ShowMessage(notInCacheMsg, "Humble Keys");
                    return;
                }

                PlayniteApi.MainView.UIDispatcher.Invoke(() =>
                {
                    const int maxRetries = 10;
                    const int delayMs = 100;
                    for (int i = 0; i < maxRetries; i++)
                    {
                        try
                        {
                            // Passing false prevents WPF from calling OleFlushClipboard(), which triggers
                            // CLIPBRD_E_CANT_OPEN when Windows Clipboard History (Win+V) or clipboard viewers
                            // lock the clipboard immediately upon receiving WM_CLIPBOARDUPDATE.
                            System.Windows.Clipboard.SetDataObject(key, false);
                            return;
                        }
                        catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException || ex is System.Runtime.InteropServices.ExternalException)
                        {
                            // Verify if the key was already placed onto the clipboard before the exception occurred
                            try
                            {
                                if (System.Windows.Clipboard.ContainsText() && System.Windows.Clipboard.GetText() == key)
                                {
                                    return;
                                }
                            }
                            catch
                            {
                                // Clipboard may still be held open by the external viewer
                            }

                            if (i == maxRetries - 1)
                            {
                                // One final check after a short delay before propagating
                                try
                                {
                                    System.Threading.Thread.Sleep(delayMs);
                                    if (System.Windows.Clipboard.ContainsText() && System.Windows.Clipboard.GetText() == key)
                                    {
                                        return;
                                    }
                                }
                                catch
                                {
                                }
                                throw;
                            }
                            System.Threading.Thread.Sleep(delayMs);
                        }
                    }
                });
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to copy CD key to clipboard");
                PlayniteApi.Dialogs.ShowErrorMessage(e.Message, "Humble Keys");
            }
        }

        public override void Dispose()
        {
            _humbleOrderRepository?.Dispose();
            base.Dispose();
        }
    }
}