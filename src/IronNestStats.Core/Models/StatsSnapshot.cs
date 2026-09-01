using System.Collections.Generic;

namespace IronNestStats.Core.Models
{
    public sealed class StatsSnapshot
    {
        public StatsSnapshot(bool active, string missionName, string language,
            IDictionary<string, double> statistics, IList<MedalSnapshot> medals)
        {
            Active = active;
            MissionName = missionName ?? string.Empty;
            Language = language ?? "English";
            Statistics = statistics ?? new Dictionary<string, double>();
            Medals = medals ?? new List<MedalSnapshot>();
        }

        public bool Active { get; }
        public string MissionName { get; }
        public string Language { get; }
        public IDictionary<string, double> Statistics { get; }
        public IList<MedalSnapshot> Medals { get; }

        public static StatsSnapshot Inactive(string language)
        {
            return new StatsSnapshot(false, string.Empty, language,
                new Dictionary<string, double>(), new List<MedalSnapshot>());
        }
    }
}
