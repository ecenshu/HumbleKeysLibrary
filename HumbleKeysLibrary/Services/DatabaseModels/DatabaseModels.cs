using System.Collections.Generic;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace HumbleKeys.Services.DatabaseModels
{
    public class Order : Models.Order
    {
        [Indexed]
        [PrimaryKey]
        public new string gamekey { get; set; }
        
        [OneToOne("gamekey", "order", CascadeOperations = CascadeOperation.All)]
        public new Product product {get; set;}
        
        [OneToMany("gamekey", "subproducts", CascadeOperations = CascadeOperation.All)]
        public new List<SubProduct> subproducts { get; set; }

        public class Product : Models.Order.Product
        {
            [OneToOne("gamekey", "product", CascadeOperations = CascadeOperation.CascadeRead)]
            public Order order { get; set; }

            public new string category { get; set; }
            public new string machine_name { get; set; }
            public new string human_name { get; set; }
            public new string choice_url { get; set; }
            public new bool is_subs_v2_product { get; set; }
            public new bool is_subs_v3_product { get; set; }
        }

        public class SubProduct : Models.Order.SubProduct
        {
            public class Download :  Models.Order.SubProduct.Download
            {
                public class DownloadStruct :  Models.Order.SubProduct.Download.DownloadStruct
                {
                    public class Url
                    {
                        public string web;
                        public string bittorrent;
                    }

                    public string human_size;
                    public string name;
                    public string sha1;
                    public ulong file_size;
                    public string md5;
                    public Url url;
                }

                public List<DownloadStruct> download_struct;
                public string machine_name;
                public string platform;
                public bool android_app_only;
            }

            public new string machine_name { get; set; }
            public new string url { get; set; }
            
            [OneToMany("gamekey", "subproducts", CascadeOperations = CascadeOperation.All)]
            public new List<Download> downloads { get; set; }
            public new string human_name { get; set; }
            public new string icon { get; set; }
            public new string library_family_name { get; set; }
        }
    }
}