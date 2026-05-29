using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HumbleKeys.Models;
using HumbleKeys.Services.GameKey.Models;
using Playnite.SDK;
using Order = HumbleKeys.Services.GameKey.Models.Order;

namespace HumbleKeys.Services
{
    public class HumbleOrderCachedRepository : IHumbleOrderRepository
    {
        private readonly IHumbleOrderRepository nextSource;
        private readonly IHumbleOrderRepository sourceRepository;
        private readonly bool prioritiseExistingRecord;
        private readonly ILogger logger;

        public HumbleOrderCachedRepository(IHumbleOrderRepository sourceRepository, IHumbleOrderRepository nextSource, bool prioritiseExistingRecord = false, ILogger logger = null) : this(nextSource)
        {
            this.sourceRepository = sourceRepository;
            this.prioritiseExistingRecord = prioritiseExistingRecord;
            this.logger = logger;
        }

        public HumbleOrderCachedRepository(IHumbleOrderRepository nextSource)
        {
            this.nextSource = nextSource;
        }
        
        public void Dispose()
        {
            sourceRepository?.Dispose();
            nextSource?.Dispose();
        }

        public bool Update(Product product) => sourceRepository.Update(product);

        public IEnumerable<string> GetLibraryKeys()
        {
            var libraryKeys = sourceRepository.GetLibraryKeys();
            if (libraryKeys != null && prioritiseExistingRecord)
                return libraryKeys;
            
            
            var sourceLibraryKeys = nextSource.GetLibraryKeys();
            return sourceLibraryKeys;
        }

        public Task<IEnumerable<string>> GetLibraryKeysAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetLibraryKeys());
        }

        public IEnumerable<IOrder> FilterOrders(string[] orderIds)
        {
            var filteredOrders = sourceRepository.FilterOrders(orderIds);
            if (filteredOrders != null)
            {
                return filteredOrders;
            }

            var nextOrders = nextSource.FilterOrders(orderIds);
            var filterOrders = nextOrders.ToList();
            foreach (var nextOrder in filterOrders)
            {
                sourceRepository.Update(nextOrder);
            }

            return filterOrders;
        }

        public bool Update(ITpk record)
        {
            throw new NotImplementedException();
        }

        public IOrder GetOrder(string id, bool retrieveLinkedRecords = false)
        {
            return GetOrderAsync(id, retrieveLinkedRecords).Result;
        }

        public async Task<IOrder> GetOrderAsync(string id, bool retrieveLinkedRecords = false, CancellationToken cancellationToken = default)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var order = await sourceRepository.GetOrderAsync(id, retrieveLinkedRecords, cancellationToken);
            stopwatch.Stop();
            logger?.Trace($"sourceRepository::GetOrderAsync returned in {stopwatch.Elapsed.ToString()}");
            
            if (order != null)
            {
                if (order.IsComplete || prioritiseExistingRecord)
                {
                    return order;      
                }
            }

            stopwatch.Reset();
            stopwatch.Start();
            var sourceOrder = await nextSource.GetOrderAsync(id, retrieveLinkedRecords, cancellationToken);
            stopwatch.Stop();
            logger?.Trace($"nextSource::GetOrderAsync returned in {stopwatch.Elapsed.ToString()}");
            // check if data was changed
            if (sourceOrder != order as Models.Order)
            {
                stopwatch.Reset();
                stopwatch.Start();
                sourceRepository.Update(sourceOrder);
                stopwatch.Stop();
                logger?.Trace($"GetOrder::Update() returned in {stopwatch.Elapsed.ToString()}");

            }

            return sourceOrder;
        }

        public Product GetProductById(string id)
        {
            throw new NotImplementedException();
        }

        public bool Update(IOrder order)
        {
            throw new NotImplementedException();
        }

        public bool Update(SubProduct orderSubproduct)
        {
            throw new NotImplementedException();
        }

        public bool Update(Download download)
        {
            throw new NotImplementedException();
        }

        public bool Update(Download.DownloadStruct downloadStruct)
        {
            throw new NotImplementedException();
        }

        public bool Update(Order.TpkdDict tpkdDict)
        {
            throw new NotImplementedException();
        }

        public bool Update(Order.TpkdDict.Tpk tpk)
        {
            throw new NotImplementedException();
        }

        public bool IsStale()
        {
            throw new NotImplementedException();
        }

        public bool IsUnprocessedOrders(string orderId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetCompleteOrderKeys()
        {
            throw new NotImplementedException();
        }

        public IChoiceMonth GetHumbleChoice(IOrder order)
        {
            var choiceMonth = sourceRepository.GetHumbleChoice(order);
            if (choiceMonth != null)
            {
                if (!choiceMonth.ChoicesRemaining || prioritiseExistingRecord)
                {
                    return choiceMonth;
                }
            }

            var sourceChoiceMonth = nextSource.GetHumbleChoice(order);
            if (choiceMonth != sourceChoiceMonth)
            {
                sourceRepository.Update(order, sourceChoiceMonth);
            }

            return sourceChoiceMonth;
        }

        public void Update(IOrder sourceOrder, IChoiceMonth sourceChoiceMonth)
        {
            throw new NotImplementedException();
        }

        public async Task<IChoiceMonth> GetHumbleChoiceAsync(IOrder order, CancellationToken cancellationToken = default)
        {
            var humbleChoice = await sourceRepository.GetHumbleChoiceAsync(order, cancellationToken);
            if (humbleChoice != null)
            {
                if (prioritiseExistingRecord)
                {
                    return humbleChoice;
                }
            }

            var sourceHumbleChoice = await nextSource.GetHumbleChoiceAsync(order, cancellationToken);
            sourceRepository.Update(order, sourceHumbleChoice);
            return sourceHumbleChoice;
        }
    }
}