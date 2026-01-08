using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HumbleKeys.Models;
using Order = HumbleKeys.Services.GameKey.Models.Order;

namespace HumbleKeys.Services
{
    public interface IDataProvider<T>
    {
        T GetData();
        Task<T> GetDataAsync(CancellationToken token = default);
    }
    
    /// <summary>
    /// Provider allows for instantiating and accessing directly with internal DI resolution via Pure DI
    /// </summary>
    public interface IHumbleOrdersProvider
    {
        /// <summary>
        /// Return a list of gamekeys which may have unclaimed/unredeemed keys
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        Task<List<string>> GetLibraryKeysAsync(CancellationToken token = default);

        Task<IEnumerable<IOrder>> GetOrdersAsync(CancellationToken token = default);
        //Order GetOrder(string gameKey);
    }
    
    public interface IOrdersProvider
    {
        IOrder GetOrder(string orderId);
        Task<IOrder> GetOrderAsync(string orderId, CancellationToken cancellationToken = default);
        ICollection<IOrder> GetOrders(ICollection<string> orderIds);
        Task<ICollection<IOrder>> GetOrdersAsync(ICollection<string> orderIds, CancellationToken cancellationToken = default);
    }
}