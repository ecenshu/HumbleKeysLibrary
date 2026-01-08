using System.Collections.Generic;
using System.Linq;
using Playnite.SDK.Data;

namespace HumbleKeys.Models
{
    public class ChoiceMonthV2 : IChoiceMonth
    {
        public class ContentChoiceOptions
        {
            public class ContentChoicesMadeDataContainer
            {
                public class ContentChociesMadeData
                {
                    [SerializationPropertyName("choices_made")]
                    public List<string> ChoicesMade { get; set; }
                }
                [SerializationPropertyName("initial")]
                public ContentChociesMadeData ChociesMadeData { get; set; }

                [SerializationPropertyName("initial-get-all-games")]
                public ContentChociesMadeData ChociesMadeDataGetAllGames { get; set; }
            }

            public class ContentChoiceDataContainer
            {
                public class ContentChoiceData
                {
                    public Dictionary<string, IContentChoice> content_choices { get; set; }
                    [SerializationPropertyName("total_choices")]
                    public int TotalChoices { get; set; }
                    [SerializationPropertyName("title")]
                    public string Title { get; set; }
                }
                [SerializationPropertyName("initial")]
                public ContentChoiceData initial { get; set; }
                [SerializationPropertyName("initial-get-all-games")]
                public ContentChoiceData initialGetAllGames { get; set; }

                public string Title => Data.Title;
                public ContentChoiceData Data => initial ?? initialGetAllGames;
            }
            [SerializationPropertyName("gamekey")]
            public string gamekey { get; set; }

            [SerializationPropertyName("title")]
            public string title { get; set; }

            public ContentChoiceDataContainer contentChoiceData { get; set; }
            public ContentChoicesMadeDataContainer contentChoicesMade { get; set; }
        }

        public ContentChoiceOptions contentChoiceOptions { get; set; }

        public string GameKey => contentChoiceOptions.gamekey;

        public string Title => contentChoiceOptions.title;
        public Dictionary<string,IContentChoice> ContentChoices => contentChoiceOptions.contentChoiceData.initial?.content_choices??contentChoiceOptions.contentChoiceData.initialGetAllGames.content_choices;

        public int TotalChoices => contentChoiceOptions.contentChoiceData.initial?.TotalChoices ??
                                   contentChoiceOptions.contentChoiceData.initialGetAllGames.TotalChoices;
        public ICollection<string> ChoicesMade
        {
            get {
                if (contentChoiceOptions.contentChoicesMade == null) return new List<string>();
                if (contentChoiceOptions.contentChoicesMade.ChociesMadeDataGetAllGames != null)
                {
                    return contentChoiceOptions.contentChoicesMade.ChociesMadeDataGetAllGames.ChoicesMade;
                }

                return contentChoiceOptions.contentChoicesMade.ChociesMadeData != null ? contentChoiceOptions.contentChoicesMade.ChociesMadeData.ChoicesMade : new List<string>();
            }
        }

        public bool ChoicesRemaining => ChoicesMade.Count < TotalChoices;

    }
}