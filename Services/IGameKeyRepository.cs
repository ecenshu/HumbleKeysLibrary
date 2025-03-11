using HumbleKeys.Models;

namespace HumbleKeys.Services
{
    public interface IGameKeyRepository
    {
        
    }

    public class GameKeyRepository : IGameKeyRepository
    {
        private readonly IHumbleKeysAccountClientSettings accountClientSettings;

        public GameKeyRepository(IHumbleKeysAccountClientSettings accountClientSettings)
        {
            this.accountClientSettings = accountClientSettings;
        }

        public bool Update(GameKeyRecord record)
        {
            return true;
        }
    }

    public class GameKeyRecord
    {
        public string Name { get; set; }
        public string GameKey { get; set; }
        public string Store {get; set;}
        public bool Redeemed { get; set; }
    }
}