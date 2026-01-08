using System.Collections.Generic;
using System.Linq;
using HumbleKeys.Models;
using Newtonsoft.Json;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace HumbleKeys.Services.GameKey.Models
{
    [Table("Subproducts")]
    public record SubProduct : ISubProduct
    {
        public struct SubProductKey
        {
            public string gamekey;
            public int element_number;
            public string machine_name;
        }
        [ForeignKey(typeof(Order))]
        [JsonIgnore]
        public string gamekey { get; set; }
        [PrimaryKey]
        [AutoIncrement]
        [JsonIgnore]
        public int id { get; set; }
        [JsonIgnore]
        public int element_number { get; set; }
        public string machine_name { get; set; }
        public string url {get; set; }
        
        [OneToMany("subproduct_id",null, CascadeOperations = CascadeOperation.All)]
        [JsonIgnore]
        public List<Download> persisted_downloads {get; set; }

        [Ignore]
        public ICollection<IDownload> downloads
        {
            get { return new List<IDownload>(persisted_downloads); }
            set { persisted_downloads = new List<Download>(); }
        }

        public string human_name {get; set; }
        public string icon {get; set; }
        public string library_family_name { get; set; }
        
        public SubProduct() {}

        public SubProduct(string gamekey, int element_number, ISubProduct subproduct)
        {
            this.gamekey = gamekey;
            this.element_number = element_number;
            url = subproduct.url;
            machine_name = subproduct.machine_name;
            human_name = subproduct.human_name;
            icon = subproduct.icon;
            library_family_name = subproduct.library_family_name;
            if (subproduct.downloads != null)
            {
                downloads = new List<IDownload>();
                for (int i = 0; i < subproduct.downloads.Count; i++)
                {
                    var download = subproduct.downloads.ElementAt(i);
                    downloads.Add(new Download(gamekey, element_number, new Download.OptionsDict(), i, download));
                }
            }
        }

        public SubProduct(ICollection<Download> downloads)
        {
            this.downloads = new List<IDownload>(downloads);
        }

        public void UpdateValues(SubProductKey subProductKey, ISubProduct subProduct)
        {
            this.gamekey = subProductKey.machine_name;
            if (string.CompareOrdinal(this.machine_name, subProduct.machine_name) != 0)
            {
                // entry updated/moved, clear all referenced instance
                this.downloads = subProduct.downloads;
            }
            this.machine_name = subProduct.machine_name;
            this.human_name = subProduct.human_name;
            this.icon = subProduct.icon;
            this.library_family_name = subProduct.library_family_name;
        }
    }
}