using System.Threading.Tasks;
using HumbleKeys.Models;

namespace HumbleKeys.Services
{
    public interface ISteamStoreRepository
    {
        Task<SteamPackage> GetSteamPackageAsync(string packageId);
    }
}