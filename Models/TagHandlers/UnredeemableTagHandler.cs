using System;
using System.Collections.Generic;
using System.Linq;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace HumbleKeys.Models.TagHandlers
{
    public class UnredeemableTagHandler<TRequest, TResult> : TagHandler<TRequest, TResult> where TRequest : TagHandlerArgs
    {
        private readonly IPlayniteAPI api;
        private readonly List<bool> pastTags;
        private readonly Tag unredeemableTag;

        public UnredeemableTagHandler(IPlayniteAPI api)
        {
            this.api = api;
            pastTags = this.api.Database.Tags.Select(tag => HumbleKeysLibrary.PAST_TAGS.Contains(tag.Name)).ToList();
            unredeemableTag = this.api.Database.Tags.Add(HumbleKeysLibrary.UNREDEEMABLE_STR);
        }

        public override TResult Handle(TRequest request)
        {
            if (request.Game.TagIds.Contains(unredeemableTag.Id)) return default(TResult);
            
            if (request.HumbleGame.is_expired)
            {
                var existingRedemptionTagIds = request.Game.Tags?.Where(t => HumbleKeysLibrary.PAST_TAGS.Contains(t.Name)).ToList().Select(tag => tag.Id) ?? Enumerable.Empty<Guid>();

                request.Game.TagIds.RemoveAll( tagId=> existingRedemptionTagIds.Contains(tagId));
                request.Game.TagIds.Add(unredeemableTag.Id);
                return default(TResult);
            }
            return base.Handle(request);
        }
    }
}