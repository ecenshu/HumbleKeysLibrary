using System;
using System.Collections.Generic;
using System.Linq;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace HumbleKeys.Models.TagHandlers
{
    public class UnredeemableTagHandler<TRequest, TResult> : TagHandler<TRequest, TResult> where TRequest : TagHandlerArgs where TResult : TagHandlerResult
    {
        private readonly List<bool> pastTags;

        public UnredeemableTagHandler(IPlayniteAPI api): base(api.Database.Tags.Add(HumbleKeysLibrary.UNREDEEMABLE_STR))
        {
            pastTags = api.Database.Tags.Select(tag => HumbleKeysLibrary.PAST_TAGS.Contains(tag.Name)).ToList();
        }

        public override TResult Handle(TRequest request)
        {
            if (!request.HumbleGame.is_expired && !request.HumbleGame.sold_out) return base.Handle(request);
            
            if (request.Game.TagIds.Contains(CurrentTag.Id)) return new TagHandlerResult (CurrentTag.Name,false) as TResult;

            /*var result = UpdateCurrentTag(request);
            if (!result.Success)
            {
                return new TagHandlerResult(CurrentTag.Name,false) as TResult;
            }*/

            return new TagHandlerResult(CurrentTag.Name, true) as TResult;
        }
    }
}