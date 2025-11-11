using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HumbleKeys.Services;
using Playnite.SDK;
using Playnite.SDK.Data;

namespace HumbleKeys.Patterns
{
    public interface IStrategy<in T, out TResult>
    {
        TResult Execute(T data); 
    }
    
    public interface IStrategy<out TResult>
    {
        TResult Execute(); 
    }


    public class FileSystemCacheStrategy<TResult> : IStrategy<TResult> where TResult : class
    {
        private readonly IDataProvider<TResult> dataProvider;
        private readonly IFileSystemCacheOptions options;

        public FileSystemCacheStrategy(IDataProvider<TResult> dataProvider, IFileSystemCacheOptions options)
        {
            this.dataProvider = dataProvider;
            this.options = options;
        }
        
        public TResult Execute()
        {
            return ExecuteAsync().Result;
        }

        public async Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return default;

            var cacheContentAsync = await dataProvider.GetDataAsync(cancellationToken);
            return cacheContentAsync;
        }
    }

    public class HumbleWebViewScrapingStrategy<T, TResult> : IStrategy<T, TResult> where T : class
    {
        private readonly IDataProvider<T, TResult> dataProvider;

        public HumbleWebViewScrapingStrategy(IDataProvider<T, TResult> dataProvider)
        {
            this.dataProvider = dataProvider;
        }
        public TResult Execute(T data)
        {
           return dataProvider.GetDataAsync(data, CancellationToken.None ).Result;
        }
    }
    
    public class HumbleWebViewScrapingStrategy<TResult> : IStrategy<TResult>
    {
        private readonly IDataProvider<TResult> dataProvider;

        public HumbleWebViewScrapingStrategy(IDataProvider<TResult> dataProvider)
        {
            this.dataProvider = dataProvider;
        }
        public TResult Execute()
        {
            return dataProvider.GetDataAsync(CancellationToken.None).Result;
        }
    }
    
    public interface IFileSystemCacheOptions
    {
        bool CacheEnabled { get; set; }
        string CacheDirectory { get; set; }
    }

    public class FileSystemCacheOptions : IFileSystemCacheOptions
    {
        public bool CacheEnabled { get; set; }
        public string CacheDirectory { get; set; }
    }
}