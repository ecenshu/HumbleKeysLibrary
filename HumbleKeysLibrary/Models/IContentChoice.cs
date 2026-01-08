
using System.Collections.Generic;

namespace HumbleKeys.Models
{
    public interface IContentChoice
    {
        string Title { get; set; }

        ICollection<ITpk> tpkds { get; set; }

        Dictionary<string, ICollection<ITpk>> nested_choice_tpkds { get; set; }
    }
}