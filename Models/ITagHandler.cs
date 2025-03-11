using Playnite.SDK.Models;

namespace HumbleKeys.Models
{
    public interface ITagHandler<TRequest,TResult>
    {
        ITagHandler<TRequest,TResult> SetNext(ITagHandler<TRequest,TResult> handler);
        TResult Handle(TRequest request);
    }

    public abstract class TagHandler<TRequest, TResult> : ITagHandler<TRequest, TResult> where TRequest: TagHandlerArgs
    {
        private ITagHandler<TRequest, TResult> NextHandler {get; set;}
        
        public ITagHandler<TRequest, TResult> SetNext(ITagHandler<TRequest, TResult> handler)
        {
            NextHandler = handler;
            return handler;
        }

        public virtual TResult Handle(TRequest request)
        {
            if (NextHandler != null)
            {
                return NextHandler.Handle(request);
            }
            
            return default(TResult);
        }
    }

    public class TagHandlerArgs
    {
        public Game Game { get; set; }
        public Order SourceOrder { get; set; }
        public Order.TpkdDict.Tpk HumbleGame { get; set; }
    }

    // If a game is not is_virtual (which infers it was added via the bundle) and it is a choice/monthly bundle, and it doesn't have a key assigned, it is claimed but no key allocated (maybe from exhausted keys) set to 'key: Claimed'
}