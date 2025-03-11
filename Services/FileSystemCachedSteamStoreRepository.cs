using System;
using System.IO;
using System.Threading.Tasks;
using HumbleKeys.Models;
using Playnite.SDK.Data;

namespace HumbleKeys.Services
{
    public class FileSystemCachedSteamStoreRepository : ISteamStoreRepository
    {
        private readonly ISteamStoreRepository steamStoreRepository;
        public string cacheDirectory { get; set; }
        
        public FileSystemCachedSteamStoreRepository(ISteamStoreRepository steamStoreRepository, IHumbleKeysAccountClientSettings clientSettings)
        {
            cacheDirectory = clientSettings.CachePath + "\\store.steampowered.com\\packagedetails";
            this.steamStoreRepository = steamStoreRepository;
            if (!Directory.Exists($"{cacheDirectory}"))
            {
                Directory.CreateDirectory($"{cacheDirectory}");
            }
        }

        public bool CacheEnabled { get; set; }
        
        public async Task<SteamPackage> GetSteamPackageAsync(string packageId, bool forceCache = false)
        {
            var packageFileName = $"{cacheDirectory}\\{packageId}.json";
            if (forceCache)
            {
                var fileInfo = new FileInfo(packageFileName);
                if (fileInfo.Exists)
                {
                    if (DateTime.Now - fileInfo.CreationTime > TimeSpan.FromDays(7))
                    {
                        File.Delete(packageFileName);
                        fileInfo.Refresh();
                    }
                }

                if (fileInfo.Exists)
                {
                    using (var streamReader = new StreamReader(new FileStream(packageFileName, FileMode.Open, FileAccess.Read, FileShare.Read)))
                    {
                        var readToEndAsync = await streamReader.ReadToEndAsync();
                        streamReader.Close();

                        return Serialization.FromJson<SteamPackage>(readToEndAsync);
                    }
                }
            }
            var steamPackage = await steamStoreRepository.GetSteamPackageAsync(packageId);
           
            using (var streamWriter = new StreamWriter(new FileStream(packageFileName, FileMode.OpenOrCreate, FileAccess.Write)))
            {
                await streamWriter.WriteAsync(Serialization.ToJson(steamPackage));
            }

            return steamPackage;
        }

        public async Task<SteamPackage> GetSteamPackageAsync(string packageId)
        {
            return await GetSteamPackageAsync(packageId, true);
        }
    }
}