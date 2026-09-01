using System.Collections.Generic;

namespace IronNestStats.Core.Models
{
    public sealed class MedalTierSnapshot
    {
        public MedalTierSnapshot(string tier, bool achieved, double progress,
            IList<MedalConditionSnapshot> conditions)
        {
            Tier = tier ?? string.Empty;
            Achieved = achieved;
            Progress = progress < 0 ? 0 : progress > 1 ? 1 : progress;
            Conditions = conditions ?? new List<MedalConditionSnapshot>();
        }

        public string Tier { get; }
        public bool Achieved { get; }
        public double Progress { get; }
        public IList<MedalConditionSnapshot> Conditions { get; }
    }
}
