using System.Linq;
using HumbleKeys.Services;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace HumbleKeys.Models.TagHandlers
{
    public class ClaimedTagHandler<TRequest, TResult> : TagHandler<TRequest, TResult> where TRequest: TagHandlerArgs where TResult: TagHandlerResult 
    {
        private readonly HumbleKeysLibrary humbleKeysLibrary;

        public ClaimedTagHandler(IPlayniteAPI api, HumbleKeysLibrary humbleKeysLibrary): base(api.Database.Tags.Add(HumbleKeysLibrary.CLAIMED_STR))
        {
            this.humbleKeysLibrary = humbleKeysLibrary;
            CurrentTag = api.Database.Tags.Add(HumbleKeysLibrary.CLAIMED_STR);
        }

        // claimed keys can only be part of a bundle that allows for a choice (either humble monthly or humble choice) that has not had a redeemed_key allocated
        public override TResult Handle(TRequest request)
        {
            // if it is part of a bundle and is_virtual == false (means the key was not added to the order by copying it from all_tpks collection)
            if (!string.IsNullOrEmpty(request.HumbleGame.redeemed_key_val?.ToString()) ||
                !(!request.HumbleGame.is_virtual && (
                    request.SourceOrder.product.human_name.ToLowerInvariant().Contains("monthly") ||
                    request.SourceOrder.product.human_name.ToLowerInvariant().Contains("choice")))
               )
            {
                return base.Handle(request);
            }
            
            // store game key
            //humbleKeysLibrary.PersistRedeemedKey(new GameKeyRecord() { GameKey = request.HumbleGame.redeemed_key_val.ToString(), GameId = request.HumbleGame.steam_app_id, Name = request.HumbleGame.human_name });

            if (IsTagCurrent(request)) return null;

            return new TagHandlerResult(CurrentTag.Name, true) as TResult;
        }
    }
}