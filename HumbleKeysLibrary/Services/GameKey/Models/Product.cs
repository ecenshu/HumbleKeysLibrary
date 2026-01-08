using HumbleKeys.Models;
using Newtonsoft.Json;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace HumbleKeys.Services.GameKey.Models
{
    [Table("Products")]
    public record Product : IProduct
    {
        [PrimaryKey]
        [NotNull]
        [JsonIgnore]
        [ForeignKey(typeof(Order))]
        public string gamekey { get; set; }
        
        public Product() {}

        public Product(IOrder order, IProduct product)
        {
            gamekey = order?.gamekey;
            category = product.category;
            machine_name = product.machine_name;
            human_name = product.human_name;
            choice_url = product.choice_url;
            is_subs_v2_product = product.is_subs_v2_product;
            is_subs_v3_product = product.is_subs_v3_product;
        }

        public Product(IProduct value) : this(null, value)
        {
        }

        public string category { get; set; }
        public string machine_name { get; set; }
        public string human_name { get; set; }
        public string choice_url { get; set; }
        public bool is_subs_v2_product { get; set; }
        public bool is_subs_v3_product { get; set; }
    }
}