using System;
using System.Collections.Generic;
using System.Linq;
using HumbleKeys.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace HumbleKeys.Services.GameKey.Models
{
    [Table("Orders")]
    public record Order : IOrder
    {
        [Table("TpkdDicts")]
        public record TpkdDict : ITpkdDict
        {
            [PrimaryKey]
            [NotNull]
            [ForeignKey(typeof(Order))]
            [JsonIgnore]
            [Indexed]
            public string gamekey { get; set; }

            [Table("Tpks")]
            public record Tpk : ITpk
            {
                public struct TpkIdentifier
                {
                    public string gamekey { get; set; }
                    public string machine_name { get; set; }
                }
                
                public Tpk(ITpk tpk)
                {
                    gamekey = tpk.gamekey;
                    machine_name = tpk.machine_name;
                    @class = tpk.@class;
                    human_name = tpk.human_name;
                    instructions_html = tpk.instructions_html;
                    is_expired = tpk.is_expired;
                    sold_out = tpk.sold_out;
                    key_type = tpk.key_type;
                    key_type_human_name = tpk.key_type_human_name;
                    library_family_name = tpk.library_family_name;
                    redeemed_key_val = tpk.redeemed_key_val;
                    steam_app_id = tpk.steam_app_id;
                    visible = tpk.visible;
                }

                [PrimaryKey] [JsonIgnore] public int id { get; set; }

                [Indexed(Name = "TpkSecondaryKey", Order = 2, Unique = true)]
                public string machine_name { get; set; }

                [ForeignKey(typeof(TpkdDict))]
                [Indexed(Name = "TpkSecondaryKey", Order = 1, Unique = true)]
                [Indexed]
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

                [Ignore] public JToken redeemed_key_val { get; set; }

                [Ignore] public bool is_virtual { get; set; }

                [JsonIgnore]
                public string persisted_redeemed_key_val
                {
                    get => redeemed_key_val?.ToString();
                    set => redeemed_key_val = value;
                }

                public Tpk()
                {
                }

                public void UpdateValues(ITpk updatedValues)
                {
                    gamekey = updatedValues.gamekey;
                    machine_name = updatedValues.machine_name;
                    @class = updatedValues.@class;
                    human_name = updatedValues.human_name;
                    instructions_html = updatedValues.instructions_html;
                    is_expired = updatedValues.is_expired;
                    sold_out = updatedValues.sold_out;
                    key_type = updatedValues.key_type;
                    key_type_human_name = updatedValues.key_type_human_name;
                    library_family_name = updatedValues.library_family_name;
                    redeemed_key_val = updatedValues.redeemed_key_val;
                    steam_app_id = updatedValues.steam_app_id;
                    visible = updatedValues.visible;
                }
            }

            [OneToMany("gamekey", "Tpk", CascadeOperations = CascadeOperation.All)]
            [JsonIgnore]
            public List<Tpk> persisted_all_tpks { get; set; }

            [Ignore]
            public ICollection<ITpk> all_tpks
            {
                get =>
                    new List<ITpk>(persisted_all_tpks);
                set
                {
                    persisted_all_tpks = value.Select(tpk =>
                        new Tpk(tpk)).ToList();
                }
            }

            public TpkdDict()
            {
            }

            public TpkdDict(IOrder order, ITpkdDict persisted_all_tpks)
            {
                gamekey = order.gamekey;
                if (persisted_all_tpks.all_tpks == null) return;

                this.persisted_all_tpks = new List<Tpk>();
                for (var i = 0; i < persisted_all_tpks.all_tpks.Count; i++)
                {
                    this.persisted_all_tpks.Add(new Tpk(persisted_all_tpks.all_tpks.ElementAt(i)) {id = i});
                }
            }

            public void UpdateValues(string gameKey, ITpkdDict updatedValues)
            {
                if (string.CompareOrdinal(gameKey,gameKey) != 0) throw new InvalidOperationException();
                List<GameKeyIdentifier> updatedGameKeys = new List<GameKeyIdentifier>();
                foreach (var tpk in persisted_all_tpks)
                {
                    if (tpk is Tpk tpkReference)
                    {
                        // Update existing values
                        if (updatedValues.all_tpks.Count <= 0) continue;
                        
                        var updatedTpkEntry = updatedValues.all_tpks.FirstOrDefault(updatedTpk => updatedTpk.machine_name == tpk.machine_name);
                        if (updatedTpkEntry == null) continue;
                            
                        tpkReference.UpdateValues(updatedTpkEntry);
                        updatedGameKeys.Add(new GameKeyIdentifier() { gamekey = tpkReference.gamekey, machine_name = tpkReference.machine_name });
                    }
                }
                // add values that are in updatedValues
                if (updatedValues.all_tpks?.Count != persisted_all_tpks?.Count)
                {
                    if (updatedValues.all_tpks != null && updatedValues.all_tpks.Count > 0)
                    {
                        var nonUpdatedGameKeys = updatedValues.all_tpks.Where(tpk => 
                            updatedGameKeys.All(identifier => 
                            identifier.gamekey != tpk.machine_name)
                        ).ToList();
                        if (persisted_all_tpks == null) persisted_all_tpks = new List<Tpk>();
                        foreach (var tpk in nonUpdatedGameKeys)
                        {
                            var existing = persisted_all_tpks.FirstOrDefault(tpk1 => tpk1.gamekey == tpk.gamekey && tpk1.machine_name == tpk.machine_name); 
                            if (existing != null)
                            {
                                existing.UpdateValues(tpk);
                            }
                            else
                            {
                                persisted_all_tpks.Add(new Tpk(tpk));
                            }
                        }
                    }
                }
                // remove values that do not exist in updatedValues
            }
        }

        [PrimaryKey] [NotNull] public string gamekey { get; set; }
        public string uid { get; set; }

        [OneToOne(CascadeOperations = CascadeOperation.All)]
        [JsonIgnore]
        public Product persisted_product { get; set; }
        
        [Ignore]
        public IProduct product { get => persisted_product;
            set => persisted_product = new Product(value); }

        [OneToMany(CascadeOperations = CascadeOperation.All)]
        [JsonIgnore]
        public List<SubProduct> persisted_subproducts { get; set; }

        [Ignore]
        public ICollection<ISubProduct> subproducts
        {
            get => new List<ISubProduct>(persisted_subproducts);
            set
            {
                if (persisted_subproducts != null)
                {

                }
                else
                {
                    if (value != null)
                    {
                        persisted_subproducts = new List<SubProduct>();
                        for (int i = 0; i < value.Count; i++)
                        {
                            var subProduct = value.ElementAt(i);
                            persisted_subproducts.Add(new SubProduct(gamekey,i, subProduct));
                        }
                    }
                }
            }
        }

        [OneToOne("gamekey", "TpkdDict", CascadeOperations = CascadeOperation.All)]
        [JsonIgnore]
        public TpkdDict persisted_tpkd_dict { get; set; }
        [Ignore]
        public ITpkdDict tpkd_dict
        {
            get=>persisted_tpkd_dict;
            set
            {
                if (persisted_tpkd_dict == null) return;
                if (value.all_tpks == null) return;
                    
                for (var i = 0; i < value.all_tpks.Count; i++)
                {
                    var valueTpk = value.all_tpks.ElementAt(i);
                    var persistedValue = persisted_tpkd_dict.all_tpks.ElementAt(i);
                    if (persistedValue == null) continue;
                            
                    persistedValue.@class = valueTpk.@class;
                    persistedValue.human_name = valueTpk.human_name;
                    persistedValue.instructions_html = valueTpk.instructions_html;
                    persistedValue.key_type = valueTpk.key_type;
                    persistedValue.key_type_human_name = valueTpk.key_type_human_name;
                    persistedValue.library_family_name = valueTpk.library_family_name;
                    persistedValue.visible = valueTpk.visible;
                }
            }
        }

        [OneToMany("gamekey", null, CascadeOperations = CascadeOperation.All)]
        [JsonConverter(typeof(ObjectToListConverter<PathIds>))]
        [JsonIgnore]
        public List<PathIds> persisted_path_ids { get; set; }

        [Ignore]
        public ICollection<string> path_ids
        {
            get { return persisted_path_ids?.Select(pathIds => pathIds.value).ToList(); }
            set { }
        }

        public class ObjectToListConverter<T> : JsonConverter

        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (!(value is List<T> pathIds)) return;
            
                writer.WriteStartArray();
                pathIds.ForEach(pathId => writer.WriteValue(pathId.ToString()));
                writer.WriteEndArray();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            public override bool CanConvert(Type objectType)
            {
                throw new NotImplementedException();
            }
        }

        public int total_choices { get; set; }
        public int choices_remaining { get; set; }

        [Table("PathIds")]
        public record PathIds
        {
            [PrimaryKey] 
            [NotNull]
            [JsonIgnore]
            [AutoIncrement]
            public int id { get; set; }
            
            public int elementNumber {get; set;}
            [ForeignKey(typeof(Order))]
            public string gamekey { get; set; }
            
            public string value { get; set; }

            public override string ToString()
            {
                return value;
            }
        }
        
        public Order() {}
        public Order(IOrder order)
        {
            gamekey = order.gamekey;
            persisted_product = new Product(order, order.product);
            uid = order.uid;
            total_choices = order.total_choices;
            choices_remaining = order.choices_remaining;
            if (order.subproducts != null)
            {
                persisted_subproducts = new List<SubProduct>();
                for (int i = 0; i < order.subproducts.Count; i++)
                {
                    var subproduct = order.subproducts.ElementAt(i);
                    persisted_subproducts.Add(new SubProduct(gamekey, i, subproduct));
                }
            }

            if (order.tpkd_dict != null)
            {
                persisted_tpkd_dict = new TpkdDict(order, order.tpkd_dict);
            }

            if (order.path_ids != null)
            {
                persisted_path_ids = new List<PathIds>();
                for (var i = 0; i < order.path_ids.Count; i++)
                {
                    var pathId = order.path_ids.ElementAt(i);
                    persisted_path_ids.Add(new PathIds {gamekey = order.gamekey, elementNumber = i, value = pathId});
                }
            }
        }
        
        public bool ContainsProcessableKeyStatuses()
        {
            // must contain at least one product and tpkd_dict needs to have entries
            if (product == null) return false;
            switch (product.category)
            {
                case "bundle":
                {
                    if (!((tpkd_dict != null && tpkd_dict.all_tpks != null && tpkd_dict.all_tpks.Count > 0) || (subproducts != null && subproducts.Count>0))) return false;
                    break;
                }
                case "storefront":
                {
                    if (tpkd_dict == null || (tpkd_dict != null && tpkd_dict.all_tpks == null || (tpkd_dict.all_tpks != null && tpkd_dict.all_tpks.Count == 0))) return false;
                    break;
                }
                case "subscriptionplan":
                {
                    // old month to month plan
                    if (!product.is_subs_v2_product && !product.is_subs_v3_product) return true; 
                    // path_ids contains a match to another order with same path_id
                    if (tpkd_dict == null || tpkd_dict.all_tpks.Count==0) return false;
                    break;
                }
                case "subscriptioncontent":
                    if (tpkd_dict == null || tpkd_dict.all_tpks.Count==0) return false;
                    break;
                case "widget":
                    if (subproducts == null || subproducts.Count == 0 || subproducts.Any(subProduct => subProduct.downloads == null || subProduct.downloads.Count == 0)) return false;
                    break;
                default:
                    return false;
            }
            return true;
        }

        [JsonIgnore]
        private string buffer;
        
        [JsonIgnore]
        [Ignore]
        public string Buffer {
            get
            {
                if (!string.IsNullOrEmpty(buffer)) return buffer;
                buffer = JsonConvert.SerializeObject(this);
                return buffer;
            }
            set { }
        }

        public bool IsComplete { get; }

        public void UpdateValues(IOrder newOrder)
        {
            // Update datatype values
            gamekey = newOrder.gamekey;
            total_choices = newOrder.total_choices;
            choices_remaining = newOrder.choices_remaining;
            uid = newOrder.uid;
            if (persisted_tpkd_dict is TpkdDict tpkdDictReference)
            {
                tpkdDictReference.UpdateValues(newOrder.gamekey, newOrder.tpkd_dict);
            }

            if (newOrder.subproducts != null)
            {
                // truncate list to match newOrder.subproducts
                if (newOrder.subproducts.Count < subproducts.Count)
                {
                    var difference = subproducts.Count - newOrder.subproducts.Count;
                    subproducts.ToList().RemoveRange(subproducts.Count - difference, difference);
                }

                for (var elementNumber = 0; elementNumber < newOrder.subproducts.Count; elementNumber++)
                {
                    var subproduct = newOrder.subproducts.ElementAt(elementNumber);
                    var persistedSubProduct = subproducts.FirstOrDefault(subProductInstance => subProductInstance.machine_name == subproduct.machine_name);
                    if (persistedSubProduct is SubProduct subProductReference)
                    {
                        subProductReference.UpdateValues(
                            new SubProduct.SubProductKey
                            {
                                gamekey = newOrder.gamekey, element_number = elementNumber,
                                machine_name = subproduct.machine_name
                            }, subproduct);
                    }
                }
            }
            //if (this.subproducts != null)
            // update references recursively
        }
    }
}