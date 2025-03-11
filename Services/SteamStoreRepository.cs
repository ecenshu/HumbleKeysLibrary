using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HumbleKeys.Models;
using Playnite.SDK.Data;

namespace HumbleKeys.Services
{
    public class SteamStoreRepository : ISteamStoreRepository
    {
        private readonly HttpClient httpClient;

        public SteamStoreRepository()
        {
            httpClient = new HttpClient() {BaseAddress = new Uri("https://store.steampowered.com/api/") };
        }

        public static SteamStoreRepository Instance { get; } = new SteamStoreRepository();

        public async Task<SteamPackage> GetSteamPackageAsync(string packageId)
        {
            var task = await httpClient.GetAsync(new Uri($"packagedetails?packageids={packageId}", UriKind.Relative));
            if (!task.IsSuccessStatusCode) return null;
            
            var steamPackageJson = await task.Content.ReadAsStringAsync();
            var steamPackageResponse = Serialization.FromJson<Dictionary<string,SteamPackageResponse>>(steamPackageJson);
            return steamPackageResponse?.FirstOrDefault().Value.Data;
        }
    }
}