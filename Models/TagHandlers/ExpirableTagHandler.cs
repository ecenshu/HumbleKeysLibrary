using Playnite.SDK.Models;

namespace HumbleKeys.Models.TagHandlers
{
    public class ExpirableTagHandler<TRequest,TResult>: TagHandler<TRequest, TResult> where TRequest : TagHandlerArgs where TResult : TagHandlerResult
    {
        public ExpirableTagHandler(Tag currentTag) : base(currentTag)
        {
        }

        public override TResult Handle(TRequest request)
        {
            if (request.HumbleGame.num_days_until_expired > 0 && string.IsNullOrEmpty(request.HumbleGame.redeemed_key_val?.ToString())) return new TagHandlerResult(CurrentTag.Name, !IsTagCurrent(request)) as TResult;

            return base.Handle(request);
        }
    }
}