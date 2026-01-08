using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HumbleKeys.Models;
using Playnite.SDK;
using Playnite.SDK.Data;

namespace HumbleKeys.Services
{
    public interface IFileCacheProvider
    {
        bool CacheEnabled { get; }
        string LocalCachePath { get; }
        Task<string> GetCacheContentAsync(string keysCacheFilename, CancellationToken cancellationToken = default);
        Task<T> GetCacheContentAsync<T>(string keysCacheFilename, CancellationToken cancellationToken = default) where T : class;
        void CreateCacheContent(string cacheFilename, string strCacheEntry);
    }

    public class FileCacheProvider : IFileCacheProvider
    {
        public bool CacheEnabled { get; }
        public string LocalCachePath { get; }
        private readonly IHumbleKeysAccountClientSettings settings;
        private readonly ILogger logger;

        public FileCacheProvider(IHumbleKeysAccountClientSettings settings, ILogger logger)
        {
            this.settings = settings;
            this.logger = logger;
            LocalCachePath = Directory.Exists(settings.CachePath) ? settings.CachePath : new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            // initialise folder structure for local cache
            var cachePaths = new[] { "order", "membership/v2","membership/v3","membership" };
            if (settings.CacheEnabled)
            {
                var cachePathsCreationError = false;
                foreach (var cachePath in cachePaths)
                {
                    if (Directory.Exists($"{LocalCachePath}\\{cachePath}")) continue;
                    
                    try
                    {
                        Directory.CreateDirectory($"{LocalCachePath}\\{cachePath}");
                    }
                    catch (Exception)
                    {
                        cachePathsCreationError = true;
                    }
                }
                logger.Info("Cache directories prepared");
                CacheEnabled = !cachePathsCreationError;
            }
            else
            {
                /*File.Delete($"{LocalCachePath}\\gameKeys.json");
                foreach (var cachePath in cachePaths)
                {
                    if (!Directory.Exists($"{LocalCachePath}\\{cachePath}")) continue;
                    
                    var cachedFiles = Directory.EnumerateFiles($"{LocalCachePath}\\{cachePath}");
                    foreach (var cachedFile in cachedFiles)
                    {
                        File.Delete(cachedFile);
                    }
                    Directory.Delete($"{LocalCachePath}\\{cachePath}");
                }
                logger.Info("Cache cleared");*/
            }
        }

        public async Task<string> GetCacheContentAsync(string keysCacheFilename, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(keysCacheFilename)) return null;
            using (var streamReader = new StreamReader(new FileStream(keysCacheFilename, FileMode.Open)))
            {
                var cacheContent = await streamReader.ReadToEndAsync();
                streamReader.Close();
                return cacheContent;
            }
        }

        public async Task<T> GetCacheContentAsync<T>(string keysCacheFilenameOrJsonString, CancellationToken cancellationToken = default) where T : class
        {
            if (keysCacheFilenameOrJsonString[0] == '{' || keysCacheFilenameOrJsonString[0] == '[')
            {
                return Serialization.FromJson<T>(keysCacheFilenameOrJsonString);
            }
            var cacheContent = await GetCacheContentAsync(keysCacheFilenameOrJsonString, cancellationToken);
            /*return cacheContent == null
                ? null
                : JsonConvert.DeserializeObject<T>(
                    cacheContent,
                    new JsonSerializerSettings()
                    {
                        TypeNameHandling = TypeNameHandling.Auto
                    });*/
            try
            {
                return cacheContent == null ? null : Serialization.FromJson<T>(cacheContent);
            }
            catch (Exception e)
            {
                logger.Error(e, "Error serializing");
                return null;
            }
        }

        public void CreateCacheContent(string cacheFilename, string strCacheEntry)
        {
            var directory = Path.GetDirectoryName(cacheFilename);
            if (!Directory.Exists(directory))
            {
                CreateDirectories(directory);
            }
            
            try
            {
                var creationMode = FileMode.Create;
                if (File.Exists(cacheFilename)) creationMode = FileMode.Truncate; 
                using (var streamWriter = new StreamWriter(new FileStream(cacheFilename, creationMode)))
                {
                    streamWriter.Write(strCacheEntry);
                    streamWriter.Close();
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to create cache file");
            }
        }

        private static bool CreateDirectories(string cacheDirectory)
        {
            if (!Directory.Exists(cacheDirectory))
            {
                try
                {
                    var directoryInfo = Directory.CreateDirectory(cacheDirectory);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            var directoryName = Path.GetDirectoryName(cacheDirectory);
            if (Directory.Exists(directoryName)) return false;
            return CreateDirectories(directoryName);
        }

    }
}