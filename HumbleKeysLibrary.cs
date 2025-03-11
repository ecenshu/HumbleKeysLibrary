using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using HumbleKeys.Models;
using HumbleKeys.Models.TagHandlers;
using HumbleKeys.Services;

namespace HumbleKeys
{
    [LoadPlugin]
    public class HumbleKeysLibrary : LibraryPlugin
    {
        private readonly PureDependencyInjector injector;
        private ITagHandler<TagHandlerArgs,bool> tagHandler;

        #region === Constants ================

        private static readonly ILogger logger = LogManager.GetLogger();
        private const string dbImportMessageId = "humblekeyslibImportError";
        private const string humblePurchaseUrlMask = @"https://www.humblebundle.com/downloads?key={0}";
        const string steamGameUrlMask = @"https://store.steampowered.com/app/{0}";
        const string steamSearchUrlMask = @"https://store.steampowered.com/search/?term={0}";
        public const string REDEEMED_STR = "Key: Redeemed";
        public const string UNREDEEMED_STR = "Key: Unredeemed";
        public const string UNREDEEMABLE_STR = "Key: Unredeemable";
        public const string EXPIRABLE_STR = "Key: Expirable";
        public const string CLAIMED_STR = "Key: Claimed";
        public const string DUPLICATE_STR = "Key: Duplicate";
        public static readonly string[] PAST_TAGS = { REDEEMED_STR, UNREDEEMED_STR, UNREDEEMABLE_STR, EXPIRABLE_STR, CLAIMED_STR, DUPLICATE_STR, "Redeemed", "Unredeemed", "Unredeemable", "Expirable", "Claimed", "Duplicate" };
        public const string HUMBLE_KEYS_SRC_NAME = "Humble Keys";
        public const string HUMBLE_KEYS_PLATFORM_NAME = "Humble Key: ";

        #endregion

        #region === Accessors ================

        public static Guid STEAMPLUGINID { get; } = Guid.Parse("cb91dfc9-b977-43bf-8e70-55f46e410fab");
        public static Guid FANATICALPLUGINID { get; } = Guid.Parse("ef17cc27-95d4-45e7-bd49-214ba2e5f4b2");
        private HumbleKeysLibrarySettings Settings { get; set; }

        public sealed override Guid Id { get; } = Guid.Parse("62ac4052-e08a-4a1a-b70a-c2c0c3673bb9");
        public override string Name => "Humble Keys Library";

        // Implementing Client adds ability to open it via special menu in Playnite.
        public override LibraryClient Client { get; } = new HumbleKeysLibraryClient();

        #endregion

        public HumbleKeysLibrary(IPlayniteAPI api) : base(api)
        {
            Properties = new LibraryPluginProperties { CanShutdownClient = false, HasCustomizedGameImport = true };
            Settings = new HumbleKeysLibrarySettings(this);
            if (string.IsNullOrEmpty(Settings.CachePath))
            {
                Settings.CachePath = $"{PlayniteApi.Paths.ExtensionsDataPath}\\{Id}";
            }

            injector = new PureDependencyInjector();
            injector.Register(_ => new SteamStoreRepository());
            injector.Register<IHumbleKeysAccountClientSettings>(_ => Settings);
            injector.Register<ISteamStoreRepository>(di => new FileSystemCachedSteamStoreRepository(di.Resolve<SteamStoreRepository>(), di.Resolve<IHumbleKeysAccountClientSettings>()));
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return Settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new HumbleKeysLibrarySettingsView();
        }

        public override IEnumerable<Game> ImportGames(LibraryImportGamesArgs args)
        {
            // depending on settings, instantiate tagcommand factory
            var claimedTagHandler = new ClaimedTagHandler<TagHandlerArgs, bool>();
            var unredeemableTagHandler = new UnredeemableTagHandler<TagHandlerArgs, bool>(PlayniteApi);
            var redeemedTagHandler = new RedeemedTagHandler<TagHandlerArgs, bool>(this, PlayniteApi);
            unredeemableTagHandler.SetNext(claimedTagHandler).SetNext(redeemedTagHandler);
            tagHandler = unredeemableTagHandler;
            
            // load "command"s depending on options
            // how to handle storing of gamekeys for claimed games but may be unredeemed?
            // scan for all redeemed entries first, then check for duplicate entry in key library, if dupe exists, mark as claimed with link to send key via humble?
            // what about outside humble keys, bought on fanatical instead?
            // index all imports before processing since multiple pass processing may be required?
            var importedGames = new List<Game>();
            var removedGames = new List<Game>();
            Exception importError = null;

            if (!Settings.ConnectAccount)
            {
                return importedGames;
            }

            try
            {
                var orders = ScrapeOrders();
                var selectedTpkds = SelectTpkds(orders);
                ProcessOrders(orders, selectedTpkds, ref importedGames, ref removedGames);
            }
            catch (Exception e)
            {
                importError = e;
                logger.Error($"Humble Keys Library: error {e}");
            }

            if (importError != null)
            {
                logger.Error($"Humble Keys Library: importError {dbImportMessageId}");
                PlayniteApi.Notifications.Add(new NotificationMessage(
                    dbImportMessageId,
                    string.Format(PlayniteApi.Resources.GetString("LOCLibraryImportError"), Name) +
                    System.Environment.NewLine + importError.Message,
                    NotificationType.Error,
                    () => OpenSettingsView()));
            }
            else
            {
                PlayniteApi.Notifications.Remove(dbImportMessageId);
            }

            return importedGames;
        }

        public Dictionary<string, Order> ScrapeOrders()
        {
            Dictionary<string, Order> orders;
            using (var view = PlayniteApi.WebViews.CreateOffscreenView(
                       new WebViewSettings { JavaScriptEnabled = false }))
            {
                var api = new Services.HumbleKeysAccountClient(view, Settings);
                var keys = api.GetLibraryKeys();
                orders = api.GetOrders(keys, Settings.ImportChoiceKeys);
            }

            return orders;
        }

        public IEnumerable<IGrouping<string, Order.TpkdDict.Tpk>> SelectTpkds(Dictionary<string, Order> orders)
        {
            return orders.Select(kv => kv.Value)
                .SelectMany(a => a.tpkd_dict?.all_tpks)
                .Where(t => t != null
                            && Settings.keyTypeWhitelist.Contains(t.key_type)
                            && !string.IsNullOrWhiteSpace(t.gamekey)
                ).GroupBy(tpk => tpk.gamekey);
        }

        /// <summary>
        /// Adds all keys from @tpkds
        /// an Order may be a single purchase, a bundle purchase or a monthly subscription
        /// </summary>
        /// <param name="orders"></param>
        /// <param name="tpkds"></param>
        /// <param name="importedGames">List of Games added from orders</param>
        /// <param name="removedGames">List of Games removed from orders due to settings</param>
        protected void ProcessOrders(Dictionary<string, Order> orders, IEnumerable<IGrouping<string, Order.TpkdDict.Tpk>> tpkds, ref List<Game> importedGames, ref List<Game> removedGames)
        {
            var redeemedTag = PlayniteApi.Database.Tags.Add(REDEEMED_STR);
            var unredeemedTag = PlayniteApi.Database.Tags.Add(UNREDEEMED_STR);
            var unredeemableTag = PlayniteApi.Database.Tags.Add(UNREDEEMABLE_STR);

            PlayniteApi.Database.BeginBufferUpdate();
            foreach (var tpkdGroup in tpkds)
            {
                var tpkdGroupEntries = tpkdGroup.AsEnumerable();
                Tag humbleChoiceTag = null;
                var groupEntries = tpkdGroupEntries.ToList();
                if (Settings.ImportChoiceKeys && Settings.CurrentTagMethodology != "none" && groupEntries.Count() > 1)
                {
                    var isHumbleMonthly = !string.IsNullOrEmpty(orders[tpkdGroup.Key].product.choice_url);
                    if (!isHumbleMonthly)
                    {
                        if (orders[tpkdGroup.Key].product.human_name.Contains("Humble Monthly"))
                        {
                            isHumbleMonthly = true;
                            logger.Info($"Inferring humble monthly from name {orders[tpkdGroup.Key].product.human_name}");
                        }
                    }

                    if (Settings.CurrentTagMethodology == "all" || Settings.CurrentTagMethodology == "monthly" && isHumbleMonthly)
                    {
                        humbleChoiceTag = PlayniteApi.Database.Tags.Add($"Bundle: {orders[tpkdGroup.Key].product.human_name}");
                    }
                }

                var bundleContainsUnredeemableKeys = false;
                var sourceOrder = orders[tpkdGroup.Key];
                if (sourceOrder != null && sourceOrder.product.category != "storefront" && sourceOrder.total_choices > 0 && sourceOrder.product.is_subs_v2_product)
                {
                    bundleContainsUnredeemableKeys = sourceOrder.choices_remaining == 0;
                }

                // Monthly bundle has all choices made
                if (bundleContainsUnredeemableKeys && humbleChoiceTag != null)
                {
                    // search Playnite db for all games that are not included in groupEntries, these can be removed
                    var virtualOrders = groupEntries.Where(tpk => tpk.is_virtual).Select(GetGameId) ?? new List<string>();
                    var gameKeys = virtualOrders.ToList();
                    // for this bundle, get all games from the database that are not in the keys collection for this order
                    var libraryKeysNotInOrder = PlayniteApi.Database.Games
                        .Where(game =>
                            game.TagIds != null && game.TagIds.Contains(humbleChoiceTag.Id) && gameKeys.Contains(game.GameId)
                        ).ToList();
                    foreach (var game in libraryKeysNotInOrder)
                    {
                        switch (Settings.CurrentUnredeemableMethodology)
                        {
                            case "tag":
                            {
                                game.TagIds.Remove(unredeemedTag.Id);
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
                                break;
                            }
                            case "delete":
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

                var steamLibraryPlugin = PlayniteApi.Addons.Plugins.FirstOrDefault(plugin => plugin.Id == STEAMPLUGINID);

                foreach (var tpkd in groupEntries)
                {
                    var gameId = GetGameId(tpkd);
                    var gameEntry = PlayniteApi.Database.Games.FirstOrDefault(game => game.GameId == gameId && game.PluginId == Id);

                    var newGameEntry = false;
                    if (gameEntry == null)
                    {
                        if (!Settings.IgnoreRedeemedKeys || (Settings.IgnoreRedeemedKeys && !IsKeyPresent(tpkd)))
                        {
                            gameEntry = ImportNewGame(tpkd, humbleChoiceTag, sourceOrder);
                            importedGames.Add(gameEntry);
                            UpdateMetaData(gameEntry, sourceOrder, tpkd, humbleChoiceTag);
                            newGameEntry = true;
                        }
                    }

                    if (Settings.ExpirableNotification)
                    {
                        if (tpkd.num_days_until_expired > 0 && GetOrderRedemptionTagState(tpkd) == EXPIRABLE_STR)
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
                            }
                            else
                            {
                                if (!gameEntry.Notes.Contains(expiryNote))
                                {
                                    gameEntry.Notes += expiryNote;
                                }
                            }

                            PlayniteApi.Database.Games.Update(gameEntry);
                        }
                    }

                    if (Settings.UnclaimedGameNotification)
                    {

                        // If a game is expired, add tag 'Key: Unclaimable'
                        // if a game sold_out == true, add tag 'Key: Unclaimable'
                        // If a game is not is_virtual (which infers it was added via the bundle) and it is a choice/monthly bundle, and it doesn't have a key assigned, it is claimed but no key allocated (maybe from exhausted keys) set to 'key: Claimed'
                        // If a game has a game_key and the relevant library doesn't have an entry (steam, epic gog etc) then the tag will be 'Key: Unredeemed'
                        // If a game is Claimed and there exists an entry in steam/gog/epic since last added to Playnite and there are no other entries in other key stores, remove the tag 'Key: Claimed' and add the tag 'Key: Redeemed' persist the gamekey and store reference in CSV
                        // If a game has more than 1 entry matching the game name, tag all entries as 'Key: Duplicate'
                        // returns whether tags were updated or not
                        
                        var recordChanged = tagHandler.Handle(new TagHandlerArgs() { Game = gameEntry, HumbleGame = tpkd, SourceOrder = sourceOrder });
                        // key present but no matching game in steam library
                        if (tpkd.steam_app_id != null)
                        {
                            var steamGame = PlayniteApi.Database.Games.FirstOrDefault(game =>
                                (game.GameId == tpkd.steam_app_id) && (steamLibraryPlugin != null &&
                                                                       game.PluginId == steamLibraryPlugin.Id));
                            if (steamGame == null && GetOrderRedemptionTagState(tpkd) == REDEEMED_STR)
                            {
                                //var steamStoreRepository = new FileSystemCachedSteamStoreRepository(new SteamStoreRepository());
                                var steamStoreRepository = injector.Resolve<ISteamStoreRepository>();
                                var steamPackage = steamStoreRepository.GetSteamPackageAsync(tpkd.steam_app_id).Result;
                                if (steamPackage != null)
                                {
                                    steamGame = PlayniteApi.Database.Games.FirstOrDefault(game => steamPackage.Apps.Any(app => app.Id==game.GameId));
                                }
                                
                                if (steamGame == null)
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
                    }

                    if (tpkd.sold_out && GetOrderRedemptionTagState(tpkd, sourceOrder) != REDEEMED_STR)
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

                    if (newGameEntry)
                    {
                        continue;
                    }

                    if (!Settings.IgnoreRedeemedKeys)
                    {
                        var tagsUpdated = UpdateRedemptionStatus(gameEntry, tpkd, humbleChoiceTag, sourceOrder);
                        var linksUpdated = UpdateStoreLinks(gameEntry.Links, tpkd);
                        var propertiesUpdated = UpdateMetaData(gameEntry, sourceOrder, tpkd, humbleChoiceTag);

                        if (gameEntry.TagIds.Contains(redeemedTag.Id))
                        {
                            // persist game key?
//                            logger.Info($"Game redeemed: title: [{tpkd.human_name}] gamekey: [{tpkd.redeemed_key_val}]");
                        }
                        
                        if (!tagsUpdated && !linksUpdated && !propertiesUpdated) continue;

                        if (gameEntry.TagIds.Contains(unredeemableTag.Id))
                        {
                            switch (Settings.CurrentUnredeemableMethodology)
                            {
                                case "tag":
                                {
                                    PlayniteApi.Database.Games.Update(gameEntry);
                                    PlayniteApi.Notifications.Add(
                                        new NotificationMessage("HumbleKeysLibraryUpdate_" + gameEntry.Id,
                                            $"{gameEntry.Name} is no longer redeemable", NotificationType.Info,
                                            () =>
                                            {
                                                if (PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen)
                                                    return;
                                                PlayniteApi.MainView.SelectGame(gameEntry.Id);
                                            })
                                    );
                                    break;
                                }
                                case "delete":
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
                            PlayniteApi.Notifications.Add(
                                new NotificationMessage("HumbleKeysLibraryUpdate_" + gameEntry.Id,
                                    $"Tags Updated for {gameEntry.Name}: " + GetOrderRedemptionTagState(tpkd, sourceOrder), NotificationType.Info,
                                    () =>
                                    {
                                        if (PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen) return;
                                        PlayniteApi.MainView.SelectGame(gameEntry.Id);
                                    })
                            );
                        }

                    }
                    else
                    {

                        // Remove Existing Game?
                        PlayniteApi.Database.Games.Remove(gameEntry);
                        logger.Trace($"Removing game {gameEntry.Name} since Settings.IgnoreRedeemedKeys is: [{Settings.IgnoreRedeemedKeys}] and IsKeyPresent() is [{IsKeyPresent(tpkd)}]");
                    }
                }
            }

            PlayniteApi.Database.EndBufferUpdate();
        }

        private bool UpdateMetaData(Game alreadyImported, Order sourceOrder, Order.TpkdDict.Tpk tpkd, Tag humbleChoiceTag)
        {
            var createdUtcDateTime = sourceOrder.created.ToUniversalTime();
            if (alreadyImported.Added != null)
            {
                var addedUtcDateTime = alreadyImported.Added.Value.Kind != DateTimeKind.Utc ? alreadyImported.Added.Value.ToUniversalTime() : alreadyImported.Added.Value;
                var timeDifference = addedUtcDateTime - createdUtcDateTime;
                if (timeDifference.Days == 0 && timeDifference.Hours == 0 && timeDifference.Minutes == 0 && timeDifference.Seconds == 0) return false;
            }

            alreadyImported.Added = createdUtcDateTime.ToLocalTime();
            return true;
        }

        bool UpdateStoreLinks(ObservableCollection<Link> links, Order.TpkdDict.Tpk tpkd)
        {
            if (tpkd.key_type != "steam") return false;

            Link steamGameLink;
            string humanName = string.Empty;
            if (!string.IsNullOrEmpty(tpkd.steam_app_id))
            {
                steamGameLink = MakeSteamLink(tpkd.steam_app_id);
            }
            else
            {
                humanName = tpkd.human_name;
                steamGameLink = new Link("Steam", string.Format(steamSearchUrlMask, humanName.Replace(" ", "%2B")));
            }

            if (humanName.EndsWith(" Steam"))
            {
                humanName = humanName.Remove(humanName.LastIndexOf(" Steam", StringComparison.Ordinal));
            }

            if (humanName.EndsWith(" DLC"))
            {
                humanName = humanName.Remove(humanName.LastIndexOf(" DLC", StringComparison.Ordinal));
            }

            var steamLinks = links.Where((link1, i) => link1.Name == "Steam");
            var steamLinksList = steamLinks.ToList();
            var existingSteamLink = steamLinksList.FirstOrDefault();
            if (existingSteamLink == null)
            {
                links.Add(steamGameLink);
                return true;
            }

            if (!string.IsNullOrEmpty(tpkd.steam_app_id) && existingSteamLink.Url == steamGameLink.Url) return false;

            // steam link url doesn't match expected value
            if (existingSteamLink.Url != steamGameLink.Url)
            {
                existingSteamLink.Url = steamGameLink.Url;
            }
            else
            {
                return false;
            }

            return true;
        }

        Game ImportNewGame(Order.TpkdDict.Tpk tpkd, Tag groupTag = null, Order order = null)
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
                Links = new List<Link>()
            };

            // add tag reflecting redemption status
            gameInfo.Tags.Add(new MetadataNameProperty(GetOrderRedemptionTagState(tpkd, order)));

            if (!string.IsNullOrWhiteSpace(tpkd?.gamekey))
            {
                gameInfo.Links.Add(MakeLink(tpkd?.gamekey));
            }

            // adds link to humble purchase
            var links = new ObservableCollection<Link>();
            if (UpdateStoreLinks(links, tpkd))
            {
                gameInfo.Links.AddRange(links.ToList());
            }

            PlayniteApi.Database.BeginBufferUpdate();
            var game = PlayniteApi.Database.ImportGame(gameInfo, this);
            if (groupTag != null)
            {
                game.TagIds.Add(groupTag.Id);
                PlayniteApi.Database.Games.Update(game);
            }

            PlayniteApi.Database.EndBufferUpdate();
            return game;
        }

        // If a game is expired, add tag 'Key: Unclaimable'
        // if a game sold_out == true, add tag 'Key: Unclaimable'
        // If a game is not is_virtual (which infers it was added via the bundle) and it is a choice/monthly bundle, and it doesn't have a key assigned, it is claimed but no key allocated (maybe from exhausted keys) set to 'key: Claimed'
        // If a game has a game_key and the relevant library doesn't have an entry (steam, epic gog etc) then the tag will be 'Key: Unredeemed'
        // If a game is Claimed and there exists an entry in steam/gog/epic since last added to Playnite and there are no other entries in other key stores, remove the tag 'Key: Claimed' and add the tag 'Key: Redeemed' persist the gamekey and store reference in CSV
        // If a game has more than 1 entry matching the game name, tag all entries as 'Key: Duplicate'
        // returns whether tags were updated or not 
        bool UpdateRedemptionStatus(Game existingGame, Order.TpkdDict.Tpk tpkd, Tag groupTag = null, Order sourceOrder = null)
        {
            var recordChanged = false;
            if (existingGame == null)
            {
                return false;
            }

            if (!Settings.keyTypeWhitelist.Contains(tpkd.key_type))
            {
                return false;
            }

            // process tags on existingGame only if there was a change in tag status

            var existingRedemptionTagIds = existingGame.Tags?.Where(t => PAST_TAGS.Contains(t.Name)).ToList().Select(tag => tag.Id) ?? Enumerable.Empty<Guid>();
            var multipleKeySources = existingGame.Tags?.Where(t => t.Name.ToLower().Contains("monthly")).ToList();
            if (multipleKeySources != null && groupTag != null && multipleKeySources.Any() && multipleKeySources.Any(tag => tag.Id != groupTag.Id))
            {
                logger.Info($"Found multiple keys for {tpkd.human_name}");
            }

            // This creates a new Tag in the Tag Database if it doesn't already exist for 'Tag: Redeemed'
            var tagIds = existingRedemptionTagIds.ToList();

            var currentTagState = PlayniteApi.Database.Tags.Add(GetOrderRedemptionTagState(tpkd, sourceOrder));

            if (!tpkd.is_virtual && groupTag != null && sourceOrder != null && !string.IsNullOrEmpty(sourceOrder.product.choice_url) && GetOrderRedemptionTagState(tpkd, sourceOrder) == UNREDEEMED_STR)
            {
                currentTagState = PlayniteApi.Database.Tags.Add(CLAIMED_STR);
            }

            if (groupTag != null)
            {
                if (existingGame.Tags != null && existingGame.Tags.All(tag => tag.Id != groupTag.Id))
                {
                    existingGame.TagIds.Add(groupTag.Id);
                    recordChanged = true;
                }
            }

            // existingGame already tagged with correct tag state
            if (tagIds.Contains(currentTagState.Id))
            {
                recordChanged = false;
            }

            // remove all tags related to key state
            existingGame.TagIds.RemoveAll(tagId => tagIds.Contains(tagId));

            existingGame.TagIds.Add(currentTagState.Id);

            if (!string.IsNullOrEmpty(tpkd.steam_app_id))
            {
                var otherGames = PlayniteApi.Database.Games.Where(game =>
                    game.PluginId == Id &&
                    game.GameId != existingGame.GameId &&
                    game.Name == tpkd.human_name &&
                    game.Links.Any(link => link.Name == "Steam")).ToList();
                if (otherGames.Any())
                {
                    var duplicateTag = PlayniteApi.Database.Tags.Add(DUPLICATE_STR);
                    existingGame.TagIds.Add(duplicateTag.Id);
                }
            }

            return recordChanged;
        }

        #region === Helper Methods ============

        private static string GetGameId(Order.TpkdDict.Tpk tpk) => $"{tpk.machine_name}_{tpk.gamekey}";
        private static Link MakeLink(string gameKey) => new Link("Humble Purchase URL", string.Format(humblePurchaseUrlMask, gameKey));
        private static Link MakeSteamLink(string gameKey) => new Link("Steam", string.Format(steamGameUrlMask, gameKey));
        private static bool IsKeyNull(Order.TpkdDict.Tpk t) => t?.redeemed_key_val == null;
        private static bool IsKeyPresent(Order.TpkdDict.Tpk t) => !IsKeyNull(t);

        private static string GetOrderRedemptionTagState(Order.TpkdDict.Tpk t, Order order = null)
        {
            if (t.is_expired) return UNREDEEMABLE_STR;
            if (t.num_days_until_expired > 0 && !IsKeyPresent(t)) return EXPIRABLE_STR;
            return IsKeyPresent(t) ? REDEEMED_STR : (order != null && !string.IsNullOrEmpty(order.product.choice_url) && !t.is_virtual) ? CLAIMED_STR : UNREDEEMED_STR;
        }

        public bool GameExistsInLibrary(TagHandlerArgs args)
        {
            // for each supported library type, check the plugin library
            switch (args.HumbleGame.key_type)
            {
                case "steam":
                {
                    // if game exists in linked library, then set tag to redeemed
                    var steamLibraryPlugin = PlayniteApi.Addons.Plugins.FirstOrDefault(plugin => plugin.Id == STEAMPLUGINID);
                    var steamGame = PlayniteApi.Database.Games.FirstOrDefault(game =>
                        game.GameId == args.HumbleGame.steam_app_id &&
                        steamLibraryPlugin != null &&
                        game.PluginId == steamLibraryPlugin.Id
                    );    
                    return steamGame != null;
                }
            }
            return false;
        }

        public bool GameExistsInStoreLibrary(TagHandlerArgs args)
        {
            if (args.HumbleGame.steam_app_id == null) return false;
            // steam keys may be bought from other stores
            var fanaticalLibraryPlugin = PlayniteApi.Addons.Plugins.FirstOrDefault(plugin => plugin.Id == FANATICALPLUGINID);
            var fanaticalGames = PlayniteApi.Database.Games.Where(game => fanaticalLibraryPlugin != null && game.PluginId == fanaticalLibraryPlugin.Id); 
            
            var fanaticalGame = fanaticalGames.FirstOrDefault(game =>
                game.Links != null && game.Links.Count > 0 && 
                game.Links.Any(link => link != null && link.Name == "Steam" && link.Url.Contains(args.HumbleGame.steam_app_id))
            );
            return fanaticalGame != null;
        }

        public bool PersistRedeemedKey(TagHandlerArgs args)
        {
            
            return true;
        }
        #endregion
    }
}