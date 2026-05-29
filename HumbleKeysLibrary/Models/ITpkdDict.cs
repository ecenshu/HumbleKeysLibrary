using System.Collections.Generic;

namespace HumbleKeys.Models
{
    public interface ITpkdDict
    {
        ICollection<ITpk> all_tpks { get; set; }
    }
}