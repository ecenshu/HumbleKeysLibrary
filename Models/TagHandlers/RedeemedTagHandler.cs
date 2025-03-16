using System;
using System.Collections.Generic;
using System.Linq;
using HumbleKeys.Services;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace HumbleKeys.Models.TagHandlers
{
    public class RedeemedTagHandler<TRequest, TResult> : TagHandler<TRequest, TResult> where TRequest : TagHandlerArgs where TResult : TagHandlerResult
    {
        public IPlayniteAPI Api { get; }
        private readonly HumbleKeysLibrary humbleKeysLibrary;
        private readonly IGameKeyRepository gameKeyRepository;
        private IHumbleKeysAccountClientSettings AccountClientSettings => humbleKeysLibrary.GetSettings(false) as IHumbleKeysAccountClientSettings;
        private readonly ILogger logger;

        public RedeemedTagHandler(HumbleKeysLibrary humbleKeysLibrary, IPlayniteAPI api, IGameKeyRepository gameKeyRepository): base(api.Database.Tags.Add(HumbleKeysLibrary.REDEEMED_STR))
        {
            logger = LogManager.GetLogger("extension");
            Api = api;
            this.humbleKeysLibrary = humbleKeysLibrary;
            this.gameKeyRepository = gameKeyRepository;
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
            
            if(request.HumbleGame.is_gift) return new TagHandlerResult(CurrentTag.Name, !IsTagCurrent(request)) as TResult;

            var stores = new List<string>();
            if (stores == null) throw new ArgumentNullException(nameof(stores));
            
            // if game has a redeemed_key in humble, and it is also in other key libraries, cannot determine which key was used
            if (humbleKeysLibrary.GameExistsInLibrary(request))
            {
                stores.Add("steam");
                if (humbleKeysLibrary.GameExistsInStoreLibrary(request))
                {
                    stores.Add("other");
                    logger.Info($"Game exists in other key store: game: {request.HumbleGame.human_name}  steam_id: {request.HumbleGame.steam_app_id}");
                    
                    // add store to gamekey repo
                }

                var gameKeyRecord = gameKeyRepository.GetById(request.SourceOrder.gamekey + "_" + request.Game.Name);
                if (gameKeyRecord != null)
                {
                    gameKeyRecord.Redeemed = true;
                    gameKeyRecord.Stores = stores.ToArray();
                    gameKeyRepository.Update(gameKeyRecord);
                }

                return new TagHandlerResult(CurrentTag.Name, !IsTagCurrent(request)) as TResult;
            }

            return base.Handle(request);
        }
    }
}