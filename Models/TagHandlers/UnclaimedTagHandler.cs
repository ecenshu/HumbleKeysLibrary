using Playnite.SDK.Models;

namespace HumbleKeys.Models.TagHandlers
{
    public class UnclaimedTagHandler<TRequest,TResult>: TagHandler<TRequest,TResult> where TRequest : TagHandlerArgs where TResult : TagHandlerResult
    {
        public UnclaimedTagHandler(Tag currentTag) : base(currentTag)
        {
        }

        public override TResult Handle(TRequest request)
        {
            if (request.SourceOrder.is_claimable_bundle && request.HumbleGame.is_virtual && string.IsNullOrEmpty(request.HumbleGame.redeemed_key_val?.ToString())) return new TagHandlerResult(CurrentTag.Name, !IsTagCurrent(request)) as TResult;
            
            return base.Handle(request);
        }
    }
}