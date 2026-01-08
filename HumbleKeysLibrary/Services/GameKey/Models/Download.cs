using System.Collections.Generic;
using HumbleKeys.Models;
using Newtonsoft.Json;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace HumbleKeys.Services.GameKey.Models
{
    [Table("Downloads")]
    public record Download : IDownload
    {
        [Table("DownloadStructs")]
        public record DownloadStruct : IDownloadStruct
        {
            [Table("Urls")]
            public record Url : IUrl
            {
                [PrimaryKey]
                [JsonIgnore]
                [AutoIncrement]
                public int id { get; set; }
                [ForeignKey(typeof(DownloadStruct))]
                [JsonIgnore]
                public int downloadstruct_id { get; set; }
                
                public string web {get; set; }
                public string bittorrent {get; set; }
            }
            
            [JsonIgnore]
            public string gamekey { get; set; }
            [JsonIgnore]
            [AutoIncrement]
            [PrimaryKey]
            public int id { get; set; }
            [ForeignKey(typeof(Download))]
            [JsonIgnore]
            public int download_id { get; set; }
            public string human_size {get; set; }
            public string name {get; set; }
            public string sha1 {get; set; }
            public ulong file_size {get; set; }
            public string small { get; set; }
            public string md5 {get; set; }
            [OneToMany("downloadstruct_id", "Url", CascadeOperations = CascadeOperation.All)]
            public IUrl url {get; set; }

            [JsonConstructor]
            public DownloadStruct(Url url)
            {
                this.url = url;
            }
            
            public DownloadStruct() {}
        }

        [Table("OptionsDict")]
        public record OptionsDict : IOptionsDict
        {
            [PrimaryKey]
            public int id { get; set; }
            [ForeignKey(typeof(Download))]
            public int download_id { get; set; }
        }

        [JsonIgnore]
        public string gamekey {get; set; }

        [JsonIgnore]
        [ForeignKey(typeof(SubProduct))]
        public int subproduct_id {get; set; }
        
        [PrimaryKey]
        [AutoIncrement]
        [JsonIgnore]
        public int id {get; set; }
        [OneToMany("downloadstruct_id", "DownloadStruct", CascadeOperations = CascadeOperation.All)]
        [JsonIgnore]
        public List<DownloadStruct> persisted_download_struct { get; set; }

        [Ignore]
        public ICollection<IDownloadStruct> download_struct { get; set; }

        [OneToOne("download_id")]
        public IOptionsDict options_dict { get; set; }
        public string download_identifier { get; set; }

        public bool desktop_app_only { get; set; }
        public string machine_name { get; set; }
        public string platform { get; set; }
        public string download_version_number { get; set; }
        public bool android_app_only { get; set; }

        public Download() {}

        public Download(string gamekey, int subproduct_id, OptionsDict options_dict, int downloadId, IDownload download)
        {
            this.gamekey = gamekey;
            this.subproduct_id = subproduct_id;
            id = downloadId;
            this.options_dict = options_dict;
            machine_name = download.machine_name;
            android_app_only = download.android_app_only;
            platform = download.platform;
        }
        [JsonConstructor]
        public Download(List<DownloadStruct> downloadStruct)
        {
            if (downloadStruct != null)
            {
                download_struct = new List<IDownloadStruct>(download_struct);
            }
        }
    }
}