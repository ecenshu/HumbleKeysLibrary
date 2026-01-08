using System;
using SQLite;

namespace HumbleKeys.Services
{
    [Table("PersistentStoreStatus")]
    class PersistentStoreStatus
    {
        public DateTime last_update { get; set; }
    }
}