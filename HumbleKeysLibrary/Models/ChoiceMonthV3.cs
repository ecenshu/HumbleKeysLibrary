using System.Collections.Generic;
using System.Linq;
using Playnite.SDK.Data;

namespace HumbleKeys.Models
{
    public class ChoiceMonthV3 : IChoiceMonth
    {
        public class ContentChoiceOptions
        {
            public class ContentChoicesMadeContainer
            {
                public class ContentChoicesContainer
                {
                    [SerializationPropertyName("choices_made")]
                    public List<string> ChoicesMade { get; set; }
                }

                [SerializationPropertyName("initial")]
                public ContentChoicesContainer contentChoicesContainer;

                public int TotalChoices { get; set; }
            }

            public class ContentChoiceDataContainer
            {
                public Dictionary<string,IContentChoice> game_data { get; set; }
            }

            public string gamekey { get; }

            public string title { get; }
            
            public ContentChoiceDataContainer contentChoiceData { get; set; }
            public ContentChoicesMadeContainer contentChoicesMade { get; set; }
        }

        public ContentChoiceOptions contentChoiceOptions { get; set; }
        public string GameKey => contentChoiceOptions.gamekey;
        public string Title => contentChoiceOptions.title;
        public Dictionary<string,IContentChoice> ContentChoices
        {
            get => contentChoiceOptions.contentChoiceData.game_data;
            set => contentChoiceOptions.contentChoiceData.game_data = value;
        }

        public ICollection<string> ChoicesMade
        {
            get => contentChoiceOptions.contentChoicesMade?.contentChoicesContainer?.ChoicesMade ?? new List<string>();
            set => throw new System.NotImplementedException();
        }

        public bool ChoicesRemaining => ChoicesMade.Count == ContentChoices.Count;
    }
}