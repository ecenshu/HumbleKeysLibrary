using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Playnite.SDK.Data;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace HumbleKeys.Models
{
    public class ContentChoice : IContentChoice
    {
        [SerializationPropertyName("title")]
        public string Title{get;set;}
        [Ignore]
        public ICollection<ITpk> tpkds {get;set;}
        
        [OneToMany("gamekey", null, CascadeOperations = CascadeOperation.All)]
        public List<Services.GameKey.Models.Order.TpkdDict.Tpk> tpkdsPersisted { get; set; }
        
        [Ignore]
        public Dictionary<string, ICollection<ITpk>> nested_choice_tpkds {get;set;}

        
        public ContentChoice(ICollection<ITpk> tpkds,
            Dictionary<string, ICollection<ITpk>> nested_choice_tpkds)
        {
            if (tpkds != null)
            {
                this.tpkds = new List<ITpk>(tpkds);
            }
            if (nested_choice_tpkds != null)
            {
                this.nested_choice_tpkds = nested_choice_tpkds
                    .ToDictionary<KeyValuePair<string, ICollection<ITpk>>, string, ICollection<ITpk>>(
                        kvp => kvp.Key,
                        kvp => new Collection<ITpk>(
                            new Collection<ITpk>(kvp.Value.ToList()))
                    );
            }
        }
    }
}