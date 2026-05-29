using System.Collections.Generic;

namespace HumbleKeys.Models
{
    public interface IDownload
    {
        bool desktop_app_only {get; set;}
        string machine_name { get; set; }
        ICollection<IDownloadStruct> download_struct { get; set; }
        IOptionsDict options_dict { get; set; }
        string download_identifier { get; set; }
        string platform { get; set; }
        string download_version_number { get; set; }
        bool android_app_only { get; set; }
    }

    public interface IOptionsDict
    {
    }
}