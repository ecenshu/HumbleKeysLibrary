namespace HumbleKeys.Models.TagHandlers
{
    public class ClaimedTagHandler<TRequest, TResult> : TagHandler<TRequest, TResult> where TRequest: TagHandlerArgs
    {
        public override TResult Handle(TRequest request)
        {
            if (!request.HumbleGame.is_virtual && request.SourceOrder.product.human_name.ToLowerInvariant().Contains("monthly") && !string.IsNullOrEmpty(request.HumbleGame.redeemed_key_val?.ToString()))
            {
                return default(TResult);
            }
            
            return base.Handle(request);
        }
    }
}