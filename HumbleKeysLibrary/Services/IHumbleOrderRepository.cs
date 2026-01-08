using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HumbleKeys.Models;
using HumbleKeys.Services.GameKey.Models;
using Order = HumbleKeys.Services.GameKey.Models.Order;

namespace HumbleKeys.Services
{
    public interface IHumbleOrderRepository : IDisposable
    {
        bool Update(Product product);
        IEnumerable<string> GetLibraryKeys();
        
        Task<IEnumerable<string>> GetLibraryKeysAsync(CancellationToken cancellationToken = default);
        IEnumerable<IOrder> FilterOrders(string[] orderIds);
        bool Update(ITpk record);
        IOrder GetOrder(string id, bool retrieveLinkedRecords = false);
        Task<IOrder> GetOrderAsync(string id, bool retrieveLinkedRecords = false, CancellationToken cancellationToken = default);
        Product GetProductById(string id);
        bool Update(IOrder order);
        bool Update(SubProduct orderSubproduct);
        bool Update(Download download);
        bool Update(Download.DownloadStruct downloadStruct);
        bool Update(Order.TpkdDict tpkdDict);
        bool Update(Order.TpkdDict.Tpk tpk);

        bool IsStale();
        /// <summary>
        /// Order is Unprocessed if all OrderGameKeyRecords are redeemed
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        bool IsUnprocessedOrders(string orderId);

        IEnumerable<string> GetCompleteOrderKeys();
        IChoiceMonth GetHumbleChoice(IOrder order);
        void Update(IOrder sourceOrder, IChoiceMonth sourceChoiceMonth);
        Task<IChoiceMonth> GetHumbleChoiceAsync(IOrder order, CancellationToken cancellationToken = default);
    }
}