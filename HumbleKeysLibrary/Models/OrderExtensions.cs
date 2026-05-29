using System;
using System.Collections.Generic;
using System.Linq;
using HumbleKeys.Services.GameKey.Models;

namespace HumbleKeys.Models
{
    public static class OrderExtensions
    {
        public static IEnumerable<ClaimedGameKey> ClaimedGames(this IOrder gameContainer)
        {
            if (gameContainer.tpkd_dict?.all_tpks == null || gameContainer.tpkd_dict.all_tpks.Count == 0)
                return Array.Empty<ClaimedGameKey>();

            return gameContainer.tpkd_dict.all_tpks.Where(tpk => tpk.redeemed_key_val != null).Select(tpk => new ClaimedGameKey { gamekey = tpk.gamekey, machine_name = tpk.machine_name });
        }

        public static IEnumerable<GameKeyIdentifier> GameKeys(this IOrder gameContainer)
        {
            Dictionary<string, KeyInfo> keyTypeWhitelist = new Dictionary<string, KeyInfo>
            {
                //["epic"] = new KeyInfo { Name = "Epic", SourceName = "Epic" },                  // This key type is valid so do we want to add it? I only have game dev asset keys at Epic myself right now but I assume this could be real games too
                //["epic_keyless"] = new KeyInfo { Name = "Epic keyless", SourceName = "Epic" },  // Is this even a valid key type? I just guessed it might be because Humble mentions it has keyless Epic keys
                ["gog"] = new KeyInfo { Name = "GOG", SourceName = "GOG" },
                ["nintendo_direct"] = new KeyInfo { Name = "Nintendo", SourceName = "Nintendo" },
                ["origin"] = new KeyInfo { Name = "EA", SourceName = "EA app" },
                ["origin_keyless"] = new KeyInfo { Name = "EA keyless", SourceName = "EA app" },
                ["steam"] = new KeyInfo { Name = "Steam", SourceName = "Steam" },
            };

            if (gameContainer.tpkd_dict?.all_tpks == null) return Array.Empty<GameKeyIdentifier>();
            return gameContainer.tpkd_dict.all_tpks
                .Where(tpk =>
                    keyTypeWhitelist.ContainsKey(tpk.key_type))
                .Select(tpkIdentifier =>
                    new GameKeyIdentifier() { gamekey = gameContainer.gamekey, machine_name = tpkIdentifier.machine_name });

        }

        public static bool IsChoiceOrder(this IOrder order)
        {
            return string.Equals(order.product?.category, "subscriptioncontent", StringComparison.Ordinal) &&
                   !string.IsNullOrEmpty(order.product?.choice_url);

        }

        public static IEnumerable<GameKeyIdentifier> ClaimableGames(this IOrder gameContainer)
        {
            if (gameContainer.tpkd_dict?.all_tpks == null) return Array.Empty<GameKeyIdentifier>();

            return gameContainer.tpkd_dict.all_tpks.Where(tpk =>
            {
                if (!(tpk.key_type == "steam" || tpk.key_type == "epic")) return false;
                return !tpk.is_expired && !tpk.sold_out;
            }).Select(tpk => new GameKeyIdentifier { gamekey = gameContainer.gamekey, machine_name = tpk.machine_name });
        }

        public static T MapTo<T>(this IOrder order) where T : Services.GameKey.Models.Order, new()
        {
            if (order == null) return null;

            return new T
            {
                choices_remaining = order.choices_remaining,
                gamekey = order.gamekey,
                path_ids = order.path_ids,
                product = order.product,
                persisted_path_ids = order.path_ids.Select((s, index) => new Services.GameKey.Models.Order.PathIds { elementNumber = index, gamekey = order.gamekey, value = s }).ToList(),
                persisted_product = order.product.MapTo<Product>(order.gamekey),
                persisted_tpkd_dict = order.tpkd_dict.MapTo<Services.GameKey.Models.Order.TpkdDict>(order.gamekey)
            };
        }

        public static T MapTo<T>(this IProduct product, string gameKey) where T : Product, new()
        {
            if (product == null) return null;

            return new T()
            {
                gamekey = gameKey,
                category = product.category,
                choice_url = product.choice_url,
                human_name = product.human_name,
                is_subs_v2_product = product.is_subs_v2_product,
                is_subs_v3_product = product.is_subs_v3_product,
                machine_name = product.machine_name
            };
        }

        public static T MapTo<T>(this ITpkdDict tpkd, string gameKey) where T : Services.GameKey.Models.Order.TpkdDict, new()
        {
            if (tpkd == null) return null;
            return new T
            {
                gamekey = gameKey,
                persisted_all_tpks = tpkd.all_tpks.Select(tpk => tpk.MapTo<Services.GameKey.Models.Order.TpkdDict.Tpk>()).ToList()
            };
        }

        public static T MapTo<T>(this ITpk tpk) where T : Services.GameKey.Models.Order.TpkdDict.Tpk, new()
        {
            if (tpk == null) return null;
            return new T
            {
                gamekey = tpk.gamekey,
                machine_name = tpk.machine_name,
                @class = tpk.@class,
                human_name = tpk.human_name,
                instructions_html = tpk.instructions_html,
                is_expired = tpk.is_expired,
                sold_out = tpk.sold_out,
                key_type = tpk.key_type,
                key_type_human_name = tpk.key_type_human_name,
                library_family_name = tpk.library_family_name,
                redeemed_key_val = tpk.redeemed_key_val,
                steam_app_id = tpk.steam_app_id,
                visible = tpk.visible
            };
        }
    }

    public struct ClaimedGameKey
    {
        public string gamekey { get; set; }
        public string machine_name { get; set; }
    }

    public struct GameKeyIdentifier
    {
        public string gamekey { get; set; }
        public string machine_name { get; set; }
    }
}