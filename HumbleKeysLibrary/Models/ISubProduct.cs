using System.Collections.Generic;

namespace HumbleKeys.Models
{
    public interface ISubProduct
    {
        string machine_name { get; set; }
        string url { get; set; }
        ICollection<IDownload> downloads { get; set; }
        string human_name { get; set; }
        string icon { get; set; }
        string library_family_name { get; set; }
    }
}