using System.Collections.Generic;
using HumbleKeys.Services;
using Playnite.SDK;

namespace HumbleKeys.Models
{
    public class HumbleKeysCollection:  PluginItemCollection<HumbleOrder>
    {
        public HumbleKeysCollection(string path, GameDatabaseCollection type = GameDatabaseCollection.Uknown) : base(path, type)
        {
        }
    }

    public class HumbleOrder : PluginDataBaseGameBase
    {
    }

    public class PluginItemCollection<T>
    {
        protected PluginItemCollection(string path, GameDatabaseCollection type)
        {
            throw new System.NotImplementedException();
        }
    }

    public class PluginDataBaseGame<T>
    {
        public virtual List<T> Items { get; set; }
    }

    public class HumbleGame: PluginDataBaseGameBase
    {
    }
}