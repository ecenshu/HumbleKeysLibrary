using System.Collections.Generic;

using System.Linq;
using Newtonsoft.Json;

namespace HumbleKeys.Models
{
    public class Order : IOrder
    {
        public class Product : IProduct
        {
            public string category { get; set; }
            public string machine_name { get; set; }
            public string human_name { get; set; }
            public string choice_url { get; set; }
            public bool is_subs_v2_product { get; set; }
            public bool is_subs_v3_product { get; set; }
        }

        public class SubProduct : ISubProduct
        {
            public class Download : IDownload
            {
                public class DownloadStruct : IDownloadStruct
                {
                    public class Url : IUrl
                    {
                        public string web { get; set; }
                        public string bittorrent { get; set; }
                    }

                    public string human_size { get; set; }
                    public string name { get; set; }
                    public string sha1 { get; set; }
                    public ulong file_size { get; set; }
                    public string small { get; set; }
                    public string md5 { get; set; }
                    public IUrl url { get; set; }

                    [JsonConstructor]
                    public DownloadStruct(Url url)
                    {
                        this.url = url;
                    }

                    public DownloadStruct()
                    {
                    }
                }

                public class OptionsDict : IOptionsDict
                {
                }
                public ICollection<IDownloadStruct> download_struct { get; set; }
                public IOptionsDict options_dict { get; set; }
                public string download_identifier { get; set; }
                public bool desktop_app_only { get; set; }
                public string machine_name { get; set; }
                public string platform { get; set; }
                public string download_version_number { get; set; }
                public bool android_app_only { get; set; }

                [JsonConstructor]
                public Download(ICollection<DownloadStruct> download_struct, OptionsDict options_dict)
                {
                    if (download_struct != null)
                    {
                        this.download_struct = new List<IDownloadStruct>(download_struct);
                    }
                    this.options_dict = options_dict;
                }
            }

            public string machine_name { get; set; }
            public string url { get; set; }
            public ICollection<IDownload> downloads { get; set; }
            public string human_name { get; set; }
            public string icon { get; set; }
            public string library_family_name { get; set; }

            [JsonConstructor]
            public SubProduct(ICollection<Download> downloads)
            {
                if (downloads != null)
                {
                    this.downloads = new List<IDownload>(downloads);
                }
            }
        }

        public class TpkdDict : ITpkdDict
        {

            public class Tpk : ITpk
            {
                public string machine_name { get; set; }
                public string gamekey { get; set; }
                public string key_type { get; set; }
                public bool visible { get; set; }
                public bool sold_out { get; set; }
                public string instructions_html { get; set; }
                public string key_type_human_name { get; set; }
                public string human_name { get; set; }
                public string @class { get; set; }
                public string library_family_name { get; set; }
                public string steam_app_id { get; set; }
                public bool is_expired { get; set; }
                public Newtonsoft.Json.Linq.JToken redeemed_key_val { get; set; }
                public bool is_virtual { get; set; }
            }

            public ICollection<ITpk> all_tpks { get; set; }

            [JsonConstructor]
            public TpkdDict(ICollection<Tpk> all_tpks)
            {
                if (all_tpks != null)
                {
                    this.all_tpks = new List<ITpk>(all_tpks);
                }
            }
        }

        public string gamekey { get; set; }
        public string uid { get; set; }
        public IProduct product { get; set; }
        public ICollection<ISubProduct> subproducts { get; set; }
        public ITpkdDict tpkd_dict { get; set; }

        public ICollection<string> path_ids { get; set; }

        // v3 seems to mean how many of the bundle has been selected
        // v2 seems to mean number of games available to be redeemed
        public int total_choices { get; set; }

        // v3 always 0?
        // v2 total_choices - number of games redeemed
        public int choices_remaining { get; set; }

        public bool ContainsProcessableKeyStatuses()
        {
            // must contain at least one product and tpkd_dict needs to have entries
            if (product == null) return false;
            switch (product.category)
            {
                case "bundle":
                {
                    // Order marked as completely redeemed
                    if (total_choices > 0 && choices_remaining == 0) return false;
                    
                    // Not a monthly/choice bundle, it should contain some game keys
                    if (!this.IsChoiceOrder() && !ContainsKeys()) return true;
                    var claimedGames = this.ClaimedGames();

                    var gameKeys = this.GameKeys();
                    return claimedGames.Count() > gameKeys.Count();
                }
                case "storefront":
                {
                    // must contain no tpk entries that do not have a redeemed_key_val
                    if (!ContainsKeys() || !this.ClaimableGames().Any()) return false;
                    break;
            }
                case "subscriptionplan":
                {
                    // old month-to-month plan
                    if (!product.is_subs_v2_product && !product.is_subs_v3_product) return true;
                    // path_ids contains a match to another order with same path_id
                    if (tpkd_dict == null || tpkd_dict.all_tpks.Count == 0) return false;
                    break;
                }
                case "subscriptioncontent":
                    if (tpkd_dict == null || tpkd_dict.all_tpks.Count == 0) return false;
                    break;
                case "widget":
                    if (subproducts == null || subproducts.Count == 0 || subproducts.Any(subProduct =>
                            subProduct.downloads == null || subProduct.downloads.Count == 0)) return false;
                    break;
                default:
                    return false;
            }

            return true;
        }

        private bool ContainsKeys()
        {
            return tpkd_dict?.all_tpks != null;
        }

        [JsonIgnore]
        private string buffer;
        [JsonIgnore]
        public string Buffer
        {
            get => buffer;
            set
            {
                // strip whitespace from json...
                buffer = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(value));
            }
        }

        public bool IsComplete
        {
            get
            {
                if (tpkd_dict == null) return false;
                return !tpkd_dict.all_tpks.Any(tpk => tpk.redeemed_key_val is null);
            }
        }

        [JsonConstructor]
        public Order(Product product, ICollection<SubProduct> subproducts, TpkdDict tpkd_dict, ICollection<string> path_ids)
        {
            this.product = product;
            if (subproducts != null)
            {
                this.subproducts = new List<ISubProduct>(subproducts);
            }
            else
            {
                this.subproducts = new List<ISubProduct>();
            }

            this.tpkd_dict = tpkd_dict;
            this.path_ids = path_ids;
        }

        public static Order FromJson(string json)
        {
            var order = JsonConvert.DeserializeObject<Order>(json);
            order.Buffer = json;
            return order;
        }
    }
}