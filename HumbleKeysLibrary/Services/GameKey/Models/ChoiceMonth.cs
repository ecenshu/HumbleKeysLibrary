using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HumbleKeys.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace HumbleKeys.Services.GameKey.Models
{
    [Table("ChoiceMonth")]
    public record ChoiceMonth : IChoiceMonth
    {
        [PrimaryKey]
        [JsonIgnore]
        public string GameKey { get; set; }

        public string Title { get; set; }

        [Ignore]
        public Dictionary<string, IContentChoice> ContentChoices
        {
            get
            {
                return ContentChoicesPersisted.ToDictionary<ContentChoice, string, IContentChoice>(
                    choice => choice.Id,
                    choice => new HumbleKeys.Models.ContentChoice(
                        choice.tpkds,
                        choice.nested_choice_tpkds?.ToDictionary(
                            pair => pair.Key,
                            pair => (ICollection<ITpk>)pair.Value.ToList())
                        )
                    );
            }
        }

        [JsonIgnore]
        [OneToMany("Id",null, CascadeOperations = CascadeOperation.All)]
        public List<ContentChoice> ContentChoicesPersisted { get; set; }

        [Table("ContentChoice")]
        public record ContentChoice : IContentChoice
        {
            [PrimaryKey]
            [JsonIgnore]
            public string Id { get; set; }
            public string Title { get; set; }
            
            [JsonIgnore]
            [Ignore]
            public ICollection<ITpk> tpkds { get; set; }

            [JsonIgnore]
            [OneToMany("gamekey", null, CascadeOperations = CascadeOperation.All)]
            public List<Order.TpkdDict.Tpk> tpkdsPersisted { get; set; }

            [Ignore]
            public Dictionary<string, ICollection<ITpk>> nested_choice_tpkds { get; set; }
            
            [OneToMany("gamekey", null, CascadeOperations = CascadeOperation.All)]
            public List<Order.TpkdDict.Tpk> nested_choice_tpkdsPersisted { get; set; }

            public ContentChoice(IContentChoice contentChoice)
            {
                Title = contentChoice.Title;
                nested_choice_tpkds = new Dictionary<string, ICollection<ITpk>>();
                foreach (var nestedChoiceTpkd in contentChoice.nested_choice_tpkds.Keys)
                {
                    nested_choice_tpkds.Add(nestedChoiceTpkd, contentChoice.nested_choice_tpkds[nestedChoiceTpkd]);
                }

                tpkds = new List<ITpk>();
                foreach (var contentChoiceTpkd in contentChoice.tpkds)
                {
                    tpkds.Add(contentChoiceTpkd);
                }
            }

            public ContentChoice(List<Order.TpkdDict.Tpk> tpkdsPersisted, List<Order.TpkdDict.Tpk> nested_choice_tpkdsPersisted)
            {
                this.tpkdsPersisted = tpkdsPersisted;
                this.nested_choice_tpkdsPersisted = nested_choice_tpkdsPersisted;
            }
        }

        [Ignore]
        public ICollection<string> ChoicesMade { get; set; }
        [OneToMany("Id",null, CascadeOperations = CascadeOperation.All)]
        public List<ChoicesMade> ChoicesMadePersisted { get; set; }

        public bool ChoicesRemaining { get; set; }
        
        [JsonIgnore]
        private string buffer;
        
        [JsonIgnore]
        [Ignore]
        public string Buffer {
            get
            {
                if (!string.IsNullOrEmpty(buffer)) return buffer;
                buffer = JsonConvert.SerializeObject(this);
                return buffer;
            }
            set { }
        }

        public ChoiceMonth() { }
        
        public ChoiceMonth(IChoiceMonth choiceMonth)
        {
            GameKey = choiceMonth.GameKey;
            Title = choiceMonth.Title;
        }
        
        public ChoiceMonth(List<ContentChoice> ContentChoicesPersisted, List<string> ChoicesMadePersisted)
        {
            this.ContentChoicesPersisted = ContentChoicesPersisted;
            this.ChoicesMadePersisted = ChoicesMadePersisted.Select(s => new ChoicesMade {ChoiceMade = s}).ToList();
        }
    }
    
    [Table("ChoicesMade")]
    public record ChoicesMade
    {
        [PrimaryKey]
        [JsonIgnore]
        [AutoIncrement]
        public int Id { get; set; }
        public string ChoiceMade { get; set; }
    }
}