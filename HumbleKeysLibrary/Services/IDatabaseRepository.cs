using System;
using System.Linq;
using HumbleKeys.Services.DatabaseModels;
using SQLite;

namespace HumbleKeys.Services
{
    public interface IDatabaseRepository
    {
        Order GetOrder(string gameKey);
        bool Update(Order order);
    }

    public class DatabaseRepository : IDatabaseRepository, IDisposable
    {
        private readonly SQLiteConnection database;

        public DatabaseRepository()
        {
            var databasePath = "humblekeys.db";
            database = new SQLiteConnection(databasePath);
            database.CreateTable<Order>();
            database.CreateTable<Order.Product>();
        }

        public Order GetOrder(string gameKey)
        {
            var execute = database.Query<Order>("select * from orders where game_key = '" + gameKey + "'");
            return execute.FirstOrDefault();
        }

        public bool Update(Order order)
        {
            throw new System.NotImplementedException();
        }

        public void Dispose()
        {
            database?.Dispose();
        }
    }
}