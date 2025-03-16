using System.Collections.Generic;
using System.Linq;
using Playnite.SDK.Models;

namespace HumbleKeys.Models
{
    public interface ITagHandler<TRequest,TResult>
    {
        ITagHandler<TRequest,TResult> SetNext(ITagHandler<TRequest,TResult> handler);
        TResult Handle(TRequest request);
    }

    public abstract class TagHandler<TRequest, TResult> : ITagHandler<TRequest, TResult> where TRequest: TagHandlerArgs where TResult: TagHandlerResult
    {
        protected TagHandler(Tag currentTag)
        {
            CurrentTag = currentTag;
        }

        protected Tag CurrentTag { get; set; }
        private ITagHandler<TRequest, TResult> NextHandler {get; set;}
        
        public ITagHandler<TRequest, TResult> SetNext(ITagHandler<TRequest, TResult> handler)
        {
            NextHandler = handler;
            return handler;
        }

        protected bool IsTagCurrent(TRequest request)
        {
            return request.Game.TagIds.Contains(CurrentTag.Id) && !GameContainsExcessTags(request);
        }

        protected bool GameContainsExcessTags(TRequest request)
        {
            var otherTagNames = HumbleKeysLibrary.PAST_TAGS.Where(tagName => string.CompareOrdinal(tagName, CurrentTag.Name)!=0);
            return otherTagNames.Any(otherTagName => request.Game.Tags.Select(gameTag=>gameTag.Name).Contains(otherTagName));
        }
        protected TResult UpdateCurrentTag(TRequest request)
        {
            // tag as Key: Claimed
            var priorTagIds = request.Game.Tags.Where(tag => HumbleKeysLibrary.PAST_TAGS.Contains(tag.Name)).Select(tag => tag.Id).ToList();
            // proceed to tag only if current Key: tag is "Unclaimed" or nonexistent
            if (!priorTagIds.Any()) return null;
            
            request.Game.TagIds.RemoveAll(guid => priorTagIds.Contains(guid));
            request.Game.TagIds.Add(CurrentTag.Id);
            return new TagHandlerResult(CurrentTag.Name, true) as TResult;
        }
        
        public virtual TResult Handle(TRequest request)
        {
            return NextHandler?.Handle(request);
        }
    }

    public class TagHandlerArgs
    {
        public Game Game { get; set; }
        public Order SourceOrder { get; set; }
        public Order.TpkdDict.Tpk HumbleGame { get; set; }
    }

    public class TagHandlerResult
    {
        public TagHandlerResult(string tagName, bool success)
        {
            TagName = tagName;
            Success = success;
        }

        public string TagName { get; set; }
        public bool Success { get; set; }
    }

    // If a game is not is_virtual (which infers it was added via the bundle) and it is a choice/monthly bundle, and it doesn't have a key assigned, it is claimed but no key allocated (maybe from exhausted keys) set to 'key: Claimed'
}