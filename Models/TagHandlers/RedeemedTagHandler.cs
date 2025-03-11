using System.Linq;
using HumbleKeys.Services;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace HumbleKeys.Models.TagHandlers
{
    public class RedeemedTagHandler<TRequest, TResult> : TagHandler<TRequest, TResult> where TRequest : TagHandlerArgs
    {
        public IPlayniteAPI Api { get; }
        private readonly IPlayniteAPI api;
        private readonly HumbleKeysLibrary humbleKeysLibrary;
        private IHumbleKeysAccountClientSettings accountClientSettings => humbleKeysLibrary.GetSettings(false) as IHumbleKeysAccountClientSettings;
        private readonly Tag redeemedTag;
        private readonly ILogger logger;

        public RedeemedTagHandler(HumbleKeysLibrary humbleKeysLibrary, IPlayniteAPI api)
        {
            logger = LogManager.GetLogger("extension");
            Api = api;
            this.humbleKeysLibrary = humbleKeysLibrary;
            redeemedTag = api.Database.Tags.Add(HumbleKeysLibrary.REDEEMED_STR);
        }

        public override TResult Handle(TRequest request)
        {
            // if already redeemed, no more processing required
            // if (request.Game.TagIds.Contains(redeemedTag.Id)) return default(TResult);
            
            // if HumbleGame has no cdkey, pass it along
            if (string.IsNullOrEmpty(request.HumbleGame.redeemed_key_val?.ToString()))
            {
                return base.Handle(request);
            }

            // if game has a redeemed_key in humble and it is also in other key libraries, cannot determine which key was used
            if (humbleKeysLibrary.GameExistsInLibrary(request))
            {
                if (humbleKeysLibrary.GameExistsInStoreLibrary(request))
                {
                    logger.Info($"Game exists in other key store: game: {request.HumbleGame.human_name}  steam_id: {request.HumbleGame.steam_app_id}");
                }
            }

            return default(TResult);
        }
    }
}