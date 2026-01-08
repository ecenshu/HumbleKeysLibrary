namespace HumbleKeys.Models
{
    public interface IDownloadStruct
    {
        string human_size { get; set; }
        string name { get; set; }
        IUrl url { get; set; }
        ulong file_size { get; set; }
        string small { get; set; }
        string md5 { get; set; }
        string sha1 { get; set; }
    }
}