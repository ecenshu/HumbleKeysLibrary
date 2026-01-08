using System.Collections.Generic;

namespace HumbleKeys.Models
{
    public interface IChoiceMonth
    {
        string GameKey { get; }
        string Title { get; }
        Dictionary<string, IContentChoice> ContentChoices { get; }
        
        ICollection<string> ChoicesMade { get; }
        
        bool ChoicesRemaining { get; }
    }
}