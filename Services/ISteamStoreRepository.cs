using System.Threading.Tasks;
using HumbleKeys.Models;
using Playnite.SDK.Data;

namespace HumbleKeys.Services
{
    public interface ISteamStoreRepository
    {
        Task<SteamPackage> GetSteamPackageAsync(string packageId);
    }
    
    public class SteamPackageResponse
    {
        [SerializationPropertyName("success")]
        public bool Success { get; set; }
        
        [SerializationPropertyName("data")]
        public SteamPackage Data {get;set;}
    }

    public class SteamPackage
    {
        [SerializationPropertyName("apps")]
        public SteamApp[] Apps { get; set; }

        public class SteamApp
        {
            [SerializationPropertyName("id")]
            public string Id { get; set; }
            [SerializationPropertyName("name")]
            public string Name { get; set; }
        }
    }
}