using IronNestStats.Core.Stats;

namespace IronNestStats.Melon.Game
{
    internal sealed class GameLocalizationService
    {
        private readonly ReflectionBridge _bridge;

        public GameLocalizationService(ReflectionBridge bridge)
        {
            _bridge = bridge;
        }

        public string CurrentLanguage
        {
            get
            {
                var manager = _bridge.GetStatic("Localisation.LocalisationManager", "Instance");
                var language = _bridge.Text(_bridge.Get(manager, "CurrentLanguage"));
                return string.IsNullOrEmpty(language) ? "English" : language;
            }
        }

        public bool IsChinese(string language)
        {
            return !string.IsNullOrEmpty(language) && language.IndexOf("Chinese", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public string ResolveIdentifier(object identifier, string fallback)
        {
            var text = _bridge.Text(_bridge.Invoke(identifier, "Get"));
            return string.IsNullOrEmpty(text) ? fallback : text;
        }

        public string GetGameText(string key, string fallback)
        {
            if (string.IsNullOrEmpty(key)) return fallback;
            var manager = _bridge.GetStatic("Localisation.LocalisationManager", "Instance");
            var text = _bridge.Text(_bridge.Invoke(manager, "Get", key));
            return string.IsNullOrEmpty(text) || text == key ? fallback : text;
        }

        public string GetStatLabel(string statisticId, string language)
        {
            var definition = StatCatalog.Find(statisticId);
            if (definition == null)
            {
                var formula = LocalizeFormula(statisticId, language);
                return formula ?? Humanize(statisticId);
            }
            var fallback = IsChinese(language) ? definition.SimplifiedChineseLabel : definition.EnglishLabel;
            return GetGameText(definition.GameLocalizationKey, fallback);
        }

        public string GetTierLabel(string tier, string language)
        {
            var bronze = string.Equals(tier, "Bronze", System.StringComparison.OrdinalIgnoreCase);
            var silver = string.Equals(tier, "Silver", System.StringComparison.OrdinalIgnoreCase);
            if (Contains(language, "Chinese"))
            {
                var traditional = Contains(language, "Traditional");
                return bronze ? (traditional ? "銅牌" : "铜牌") :
                    silver ? (traditional ? "銀牌" : "银牌") : (traditional ? "金牌" : "金牌");
            }
            if (Contains(language, "Japanese")) return bronze ? "銅メダル" : silver ? "銀メダル" : "金メダル";
            if (Contains(language, "Korean")) return bronze ? "동메달" : silver ? "은메달" : "금메달";
            if (Contains(language, "French")) return bronze ? "Bronze" : silver ? "Argent" : "Or";
            if (Contains(language, "German")) return bronze ? "Bronze" : silver ? "Silber" : "Gold";
            if (Contains(language, "Spanish")) return bronze ? "Bronce" : silver ? "Plata" : "Oro";
            if (Contains(language, "Italian")) return bronze ? "Bronzo" : silver ? "Argento" : "Oro";
            if (Contains(language, "Portuguese")) return bronze ? "Bronze" : silver ? "Prata" : "Ouro";
            if (Contains(language, "Russian")) return bronze ? "Бронза" : silver ? "Серебро" : "Золото";
            if (Contains(language, "Polish")) return bronze ? "Brąz" : silver ? "Srebro" : "Złoto";
            if (Contains(language, "Turkish")) return bronze ? "Bronz" : silver ? "Gümüş" : "Altın";
            return bronze ? "Bronze" : silver ? "Silver" : "Gold";
        }

        private string LocalizeFormula(string value, string language)
        {
            if (string.IsNullOrEmpty(value)) return null;
            for (var index = 1; index < value.Length - 1; index++)
            {
                var symbol = value[index];
                if (symbol != '+' && symbol != '-' && symbol != '*' && symbol != '/') continue;
                var left = value.Substring(0, index);
                var right = value.Substring(index + 1);
                var displayOperator = symbol == '/' ? " / " : symbol.ToString();
                return GetStatLabel(left, language) + displayOperator + GetStatLabel(right, language);
            }
            return null;
        }

        private static string Humanize(string value)
        {
            if (string.IsNullOrEmpty(value)) return "Value";
            var result = new System.Text.StringBuilder();
            for (var index = 0; index < value.Length; index++)
            {
                if (index > 0 && char.IsUpper(value[index]) && char.IsLower(value[index - 1])) result.Append(' ');
                result.Append(value[index]);
            }
            return result.ToString();
        }

        private static bool Contains(string value, string expected)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.IndexOf(expected, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
