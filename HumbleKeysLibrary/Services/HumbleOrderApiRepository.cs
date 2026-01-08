using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HumbleKeys.Models;
using HumbleKeys.Services.GameKey.Models;
using PlayniteExtensions.Common;
using Order = HumbleKeys.Services.GameKey.Models.Order;

namespace HumbleKeys.Services
{
    public class HumbleOrderApiRepository : IHumbleOrderRepository
    {
        private readonly HumbleKeysAccountClient api;

        public HumbleOrderApiRepository(HumbleKeysAccountClient api)
        {
            this.api = api;
            if (!api.GetIsUserLoggedIn())
            {
                throw new NotAuthenticatedException();
            }
        }
        public void Dispose()
        {
        }

        public bool Update(Product product)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<string> GetLibraryKeys()
        {
            return GetLibraryKeysAsync().Result;
        }

        public async Task<IEnumerable<string>> GetLibraryKeysAsync(CancellationToken cancellationToken = default)
        {
            return await api.GetLibraryKeysAsync(cancellationToken);
        }

        public IEnumerable<IOrder> FilterOrders(string[] orderIds)
        {
            throw new System.NotImplementedException();
        }

        public bool Update(ITpk record)
        {
            throw new System.NotImplementedException();
        }

        public IOrder GetOrder(string id, bool retrieveLinkedRecords = false)
        {
            return GetOrderAsync(id, retrieveLinkedRecords).Result;
        }

        public async Task<IOrder> GetOrderAsync(string id, bool retrieveLinkedRecords = false, CancellationToken cancellationToken = default) => await api.GetOrderAsync(id, retrieveLinkedRecords, cancellationToken);

        public Product GetProductById(string id)
        {
            throw new System.NotImplementedException();
        }

        public bool Update(IOrder order)
        {
            throw new System.NotImplementedException();
        }

        public bool Update(SubProduct orderSubproduct)
        {
            throw new System.NotImplementedException();
        }

        public bool Update(Download download)
        {
            throw new System.NotImplementedException();
        }

        public bool Update(Download.DownloadStruct downloadStruct)
        {
            throw new System.NotImplementedException();
        }

        public bool Update(Order.TpkdDict tpkdDict)
        {
            throw new System.NotImplementedException();
        }

        public bool Update(Order.TpkdDict.Tpk tpk)
        {
            throw new System.NotImplementedException();
        }

        public bool IsStale()
        {
            throw new System.NotImplementedException();
        }

        public bool IsUnprocessedOrders(string orderId)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<string> GetCompleteOrderKeys()
        {
            throw new System.NotImplementedException();
        }

        public IChoiceMonth GetHumbleChoice(IOrder order)
        {
            throw new System.NotImplementedException();
        }

        public void Update(IOrder order, IChoiceMonth sourceChoiceMonth)
        {
            throw new System.NotImplementedException();
        }

        public async Task<IChoiceMonth> GetHumbleChoiceAsync(IOrder order, CancellationToken cancellationToken = default)
        {
            (bool dbHit, List<ITpk> extras, IChoiceMonth choiceMonth) choiceMonthDataAsync = await api.GetChoiceMonthDataAsync(order, cancellationToken);
            return choiceMonthDataAsync.choiceMonth;
        }
    }
}