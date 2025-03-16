using System;
using System.IO;
using SqlNado;

namespace HumbleKeys.Services
{
    public interface IGameKeyRepository
    {
        bool Update(GameKeyRecord record);
        
        GameKeyRecord GetById(string id);
    }

    public class GameKeyRepository : IGameKeyRepository, IDisposable
    {
        private readonly IHumbleKeysAccountClientSettings accountClientSettings;
        private const string databaseFile = "gamekeys.db";
        public SQLiteDatabase Database { get; set; }

        public GameKeyRepository(IHumbleKeysAccountClientSettings accountClientSettings)
        {
            this.accountClientSettings = accountClientSettings;
            var databaseFilePath = Path.Combine(accountClientSettings.CachePath, databaseFile);
            Database = new SQLiteDatabase(databaseFilePath, SQLiteOpenOptions.SQLITE_OPEN_CREATE | SQLiteOpenOptions.SQLITE_OPEN_READWRITE);
            Database.SynchronizeSchema<GameKeyRecord>();
        }

        public bool Update(GameKeyRecord record)
        {
            return Database.Save(record, new SQLiteSaveOptions(Database));
        }

        public GameKeyRecord GetById(string id)
        {
            return Database.LoadByPrimaryKey<GameKeyRecord>(id);
        }

        public void Dispose()
        {
            Database?.Dispose();
        }
    }

    [SQLiteTable(Name = "GameKeyRecord")]
    public class GameKeyRecord
    {
        [SQLiteIndex("Id")]
        [SQLiteColumn(IsNullable =  false, IsPrimaryKey = true)]
        public string Id => OrderNumber+"_"+Name;
        [SQLiteColumn(IsNullable = false)]
        [SQLiteIndex("OrderNumber")]
        public string OrderNumber { get; set; }
        
        [SQLiteIndex("GameId")]
        public string GameId {  get; set; }

        [SQLiteColumn(IsNullable = false)]
        public string Name { get; set; }
        public string GameKey { get; set; }
        public string[] Stores {get; set;}
        public bool Redeemed { get; set; }
        
    }
}