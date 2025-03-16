using HumbleKeys.Services;
using Playnite.SDK;

namespace HumbleKeys.Models.TagHandlers
{
    public class UnredeemedTagHandler<TRequest,TResult>: TagHandler<TRequest, TResult> where TRequest : TagHandlerArgs where TResult : TagHandlerResult
    {
        private readonly IPlayniteAPI api;
        private readonly IGameKeyRepository repository;

        public UnredeemedTagHandler(IPlayniteAPI api, IGameKeyRepository repository): base(api.Database.Tags.Add(HumbleKeysLibrary.UNREDEEMED_STR))
        {
            this.api = api;
            this.repository = repository;
        }

        // only able to be unredeemed if it is NOT part of a monthly bundle type where a choice needed to be made
        // unredeemed means it has a cdkey associated but not detected in other game library where key will be consumed
        public override TResult Handle(TRequest request)
        {
            if (string.IsNullOrEmpty(request.HumbleGame.redeemed_key_val?.ToString())) return base.Handle(request);
            if (request.SourceOrder.is_claimable_bundle && request.HumbleGame.is_virtual) return base.Handle(request);
            
            //if (string.IsNullOrEmpty(request.HumbleGame.redeemed_key_val?.ToString())) return new TagHandlerResult(CurrentTag.Name, GameContainsExcessTags(request)) as TResult;
            
            // if (IsTagCurrent(request)) return null;
            //var updateResult = UpdateCurrentTag(request);
            // persist gamekey
            var update = repository.Update(new GameKeyRecord()
            {
                GameId = request.HumbleGame.steam_app_id,
                Name = request.Game.Name,
                GameKey = request.HumbleGame.redeemed_key_val.ToString(),
                OrderNumber = request.HumbleGame.gamekey
            });
            var nextResult = base.Handle(request);
            if (nextResult == null)
            {
                return new TagHandlerResult(CurrentTag.Name, !IsTagCurrent(request)) as TResult;
            }
            return nextResult;
        }
    }
}