using System;
using System.Collections.Generic;
using HumbleKeys.Models;
using Playnite.SDK;

namespace HumbleKeys.Services
{
    public class HumbleKeysDatabase : PluginDatabaseObject<HumbleKeysLibrarySettings, HumbleKeysCollection, HumbleOrder, HumbleGame>
    {
        public HumbleKeysDatabase(HumbleKeysLibrarySettings PluginSettings, string PluginUserDataPath) : base(PluginSettings, "HumbleKeysLibrary", PluginUserDataPath)
        {
            
        }

        protected override bool LoadDatabase()
        {
            throw new NotImplementedException();
        }

        public HumbleOrder Get(Guid id, bool onlyCache = false, bool force = false)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class PluginDatabaseObject<TSettings, TDatabase, TItem, T>: ObservableObject, IPluginDatabase
        where TSettings : ISettings
        where TDatabase : PluginItemCollection<TItem>
        where TItem : PluginDataBaseGameBase
    {
        protected PluginDatabaseObject(TSettings pluginSettings, string pluginName, string pluginUserDataPath)
        {
            throw new NotImplementedException();
        }

        protected virtual bool LoadDatabase()
        {
            throw new NotImplementedException();
        }
    }

    public class PluginDataBaseGameBase
    {
    }

    public interface IPluginDatabase
    {
    }
}