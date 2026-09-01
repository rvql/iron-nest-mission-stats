namespace IronNestStats.Core.Stats
{
    public enum StatFormat
    {
        Count,
        Decimal,
        Percent,
        Duration,
        Boolean
    }

    public sealed class StatDefinition
    {
        public StatDefinition(string id, string gameLocalizationKey, string englishLabel,
            string simplifiedChineseLabel, StatFormat format)
        {
            Id = id;
            GameLocalizationKey = gameLocalizationKey;
            EnglishLabel = englishLabel;
            SimplifiedChineseLabel = simplifiedChineseLabel;
            Format = format;
        }

        public string Id { get; }
        public string GameLocalizationKey { get; }
        public string EnglishLabel { get; }
        public string SimplifiedChineseLabel { get; }
        public StatFormat Format { get; }
    }
}
