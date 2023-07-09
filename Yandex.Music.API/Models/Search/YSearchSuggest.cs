namespace Yandex.Music.Api.Models.Search
{
    public class YSearchSuggest
    {
        public YSearchBest Best { get; set; }
        public List<string> Suggestions { get; set; }
    }
}