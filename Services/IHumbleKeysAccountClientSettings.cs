using Playnite.SDK;

namespace HumbleKeys.Services
{
    public interface IHumbleKeysAccountClientSettings: ISettings
    {
        bool CacheEnabled { get; set; }
        string CachePath { get; set; }
    }
}