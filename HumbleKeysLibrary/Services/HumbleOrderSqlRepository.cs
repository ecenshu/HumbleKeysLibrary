using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HumbleKeys.Models;
using HumbleKeys.Services.GameKey.Models;
using Playnite.SDK;
using SQLite;
using SQLiteNetExtensions.Exceptions;
using SQLiteNetExtensions.Extensions;
using Order = HumbleKeys.Services.GameKey.Models.Order;

namespace HumbleKeys.Services
{
    public class HumbleOrderSqlRepository : IHumbleOrderRepository
    {
        private readonly IHumbleKeysAccountClientSettings accountClientSettings;
        private readonly ILogger logger;
        private readonly SQLiteConnection connection;
        private const string databaseFile = "gamekeys.db";

        public HumbleOrderSqlRepository(IHumbleKeysAccountClientSettings accountClientSettings, ILogger logger)
        {
            this.accountClientSettings = accountClientSettings;
            this.logger = logger;
            var databaseFilePath = Path.Combine(accountClientSettings.CachePath, databaseFile);

            connection = new SQLiteConnection(databaseFilePath, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache);
            try
            {
                /*connection.DropTable<Download.DownloadStruct.Url>();
                connection.DropTable<Download.DownloadStruct>();
                connection.DropTable<Download>();
                connection.DropTable<Order.TpkdDict.Tpk>();
                connection.DropTable<Order.TpkdDict>();
                connection.DropTable<Product>();
                connection.DropTable<SubProduct>();
                connection.DropTable<Order.TpkdDict>();
                connection.DropTable<Order.PathIds>();
                connection.DropTable<Order>();
                connection.DropTable<PersistentStoreStatus>();*/
                connection.CreateTable<Download.DownloadStruct.Url>();
                connection.CreateTable<Download.DownloadStruct>();
                connection.CreateTable<Download>();
                connection.CreateIndex("DownloadSecondaryKey", "Downloads", new[] { "id", "gamekey", "subproduct_id" }, true);
                connection.CreateTable<Order.TpkdDict.Tpk>();
                connection.CreateIndex("TpkSecondaryKey", "Tpks", new[] { "gamekey", "machine_name" }, true);
                connection.CreateTable<Order.TpkdDict>();
                connection.CreateTable<Product>();
                connection.CreateTable<SubProduct>();
                connection.CreateIndex("SubProductSecondaryKey", "Subproducts", new[] { "gamekey", "element_number" }, true);
                connection.CreateTable<Order.TpkdDict>();
                connection.CreateTable<Order.PathIds>();
                connection.CreateTable<Order>();
                connection.CreateTable<PersistentStoreStatus>();
                connection.CreateTable<ContentChoice>();
                connection.CreateTable<ChoicesMade>();
                //connection.DropTable<ChoiceMonth>();
                connection.CreateTable<ChoiceMonth>();
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to create database");
            }
        }

        public bool Update(Product product)
        {
            return connection.Update(product, typeof(Product)) > 0;
        }

        public IEnumerable<string> GetLibraryKeys()
        {
            return connection.Table<Order>().Select(order => order.gamekey);
        }

        public Task<IEnumerable<string>> GetLibraryKeysAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetLibraryKeys());
        }

        public IEnumerable<IOrder> FilterOrders(string[] orderIds)
        {
            return connection.Table<Order>().Where(order => orderIds.Contains(order.gamekey));
        }

        public bool Update(ITpk record)
        {
            if (record is Models.Order.TpkdDict.Tpk)
            {
                // load record by primary key to update
                var tpk = GetTpkById(new Order.TpkdDict.Tpk.TpkIdentifier
                    { machine_name = record.machine_name, gamekey = record.gamekey });
            }
            return connection.Update(record, typeof(Order.TpkdDict.Tpk)) > 0;
        }


        public ITpk GetGameKeyRecordById(string id)
        {
            return connection.Get<Order.TpkdDict.Tpk>(id);
        }

        public IOrder GetOrder(string id, bool retrieveLinkedRecords = false)
        {
            try
            {
                var loadByPrimaryKey = connection.GetWithChildren<Order>(id, true);
                if (retrieveLinkedRecords && loadByPrimaryKey.IsChoiceOrder())
                {
                    var humbleChoice = GetHumbleChoice(loadByPrimaryKey);
                }
                return loadByPrimaryKey;
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to fetch order");
                return null;
            }
        }

        public Task<IOrder> GetOrderAsync(string id, bool retrieveLinkedRecords = false, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetOrder(id));
        }

        private Order.TpkdDict.Tpk GetTpkById(Order.TpkdDict.Tpk.TpkIdentifier identifier)
        {
            return connection.FindWithQuery<Order.TpkdDict.Tpk>("SELECT * FROM Tpks WHERE gamekey = ? AND machine_name = ?", identifier.gamekey, identifier.machine_name);
        }
        
        private ITpkdDict GetTkpdDictById(string id)
        {
            var data = new Order.TpkdDict
            {
                persisted_all_tpks = connection.Query<Order.TpkdDict.Tpk>($"SELECT * FROM Tpks WHERE gamekey = ?", id).ToList()
            };

            return data;
        }

        /*private ICollection<string> GetPathIdsById(string id)
        {
            return Database.Load<PathId>
        }*/

        private IEnumerable<SubProduct> GetSubProductsById(string id)
        {
            var loadByPrimaryKey = connection.Query<SubProduct>($"SELECT * FROM SubProducts WHERE gamekey = ? ORDER BY id ASC", id);
            var subProductsById = loadByPrimaryKey.ToList();
            foreach (var subProduct in subProductsById)
            {
                subProduct.persisted_downloads = connection.Query<Download>($"WHERE gamekey == '{id}' AND subproduct_id == '{subProduct.id}'").ToList();
                foreach (var download in subProduct.persisted_downloads)
                {
                    download.persisted_download_struct = connection.Query<Download.DownloadStruct>($"WHERE gamekey == '{id}' AND subproduct_id == '{subProduct.id}' AND download_id == '{download.id}' ORDER BY id ASC").ToList();
                }
            }
            return subProductsById;
        }

        public Product GetProductById(string id)
        {
            var loadByPrimaryKey = connection.Get<Product>(id);
            return loadByPrimaryKey;
        }
        
        public bool Update(IOrder order)
        {
            var sourceOrder = GetOrder(order.gamekey) as Order;
            if (sourceOrder == null)
            {
                var newOrder = new Order(order);
                return Update(newOrder);
            }
            
            sourceOrder.UpdateValues(order);
            return Update(sourceOrder);
        }

        
        private bool Update(Order order)
        {
            try
            {
                connection.InsertOrReplaceWithChildren(order, true);
                return true;
            }
            catch (IncorrectRelationshipException)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.Error(exception, "Unhandled Exception");
                return false;
            }  
        } 

        public bool Update(SubProduct orderSubproduct)
        {
            try
            {
                connection.InsertOrReplaceWithChildren(orderSubproduct, true);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Update(Download download)
        {
            try
            {
                connection.InsertOrReplaceWithChildren(download, true);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Update(Download.DownloadStruct downloadStruct)
        {
            try
            {
                connection.InsertOrReplaceWithChildren(downloadStruct, true);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Update(Order.TpkdDict tpkdDict)
        {
            try
            {
                connection.InsertOrReplaceWithChildren(tpkdDict, true);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool Update(Order.TpkdDict.Tpk tpk)
        {
            try
            {
                connection.InsertOrReplaceWithChildren(tpk, true);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool IsStale()
        {
            var orders = connection.Query<Order>("SELECT gamekey FROM Orders WHERE choices_remaining > 0");
            return true; //orders.Any();
        }

        /// <summary>
        /// Order is Unprocessed if all OrderGameKeyRecords are redeemed
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public bool IsUnprocessedOrders(string orderId)
        {
            if (connection.Get<Product>(orderId) is IProduct product)
            {
                var isBundle = product.is_subs_v2_product || product.is_subs_v3_product;
                if (isBundle)
                {
                    var allKeys = connection.Query<Order>($"SELECT * FROM Orders WHERE gamekey == '{orderId}'");
                }
            }
            var sqLiteStatement = connection.Query<Order.TpkdDict.Tpk>($"SELECT * FROM Tpks WHERE gamekey = '{orderId}' and persisted_redeemed_key_val is null");
            return sqLiteStatement.Any();
        }

        public IEnumerable<string> GetCompleteOrderKeys()
        {
            var allGameKeys = connection.Query<Order>("SELECT gamekey FROM Orders").Select(order => order.gamekey);
            var unclaimedGameKeys = connection.Query<Order.TpkdDict.Tpk>("SELECT Orders.gamekey FROM Tpks join Orders ON Orders.gamekey = Tpks.gamekey WHERE persisted_redeemed_key_val is null OR tPKS.gamekey IS NULL GROUP by Orders.gamekey").Select(x => x.gamekey);
            return allGameKeys.Except(unclaimedGameKeys);
            // Redeemed count for an order: "select gamekey, Count(*) as redeemed_count FROM Tpks where persisted_redeemed_key_val is not null GROUP by gamekey"
            // total games for an order: 
        }

        public IChoiceMonth GetHumbleChoice(IOrder order)
        {
            var choiceMonth = connection.Get<ChoiceMonth>(order.gamekey);
            if (order.product.is_subs_v2_product)
            {
                var mappedChoiceMonth = new ChoiceMonthV2();
                return mappedChoiceMonth;
            }
            if (order.product.is_subs_v3_product)
            {
                var mappedChoiceMonth = new ChoiceMonthV3()
                {
                    ContentChoices = choiceMonth.ContentChoices
                };
                return mappedChoiceMonth;
            }
            return null;
        }

        public void Update(IOrder sourceOrder, IChoiceMonth sourceChoiceMonth)
        {
            try
            {
                connection.Update(sourceChoiceMonth);
            }
            catch (Exception e)
            {
                logger.Error(e, "Unhandled Exception");
            }
        }

        public Task<IChoiceMonth> GetHumbleChoiceAsync(IOrder order, CancellationToken cancellationToken = default)
        {
            try
            {
                return Task.FromResult(GetHumbleChoice(order));
            }
            catch (SQLiteException sqLiteException)
            {
                if (sqLiteException.Message.StartsWith("no such table:")) return Task.FromResult<IChoiceMonth>(null);
                throw;
            }
            catch (Exception e)
            {
                logger.Error(e, "UnhandledException");
            }

            return Task.FromResult<IChoiceMonth>(null);
        }

        public void Dispose()
        {
            connection?.Dispose();
        }
    }
}