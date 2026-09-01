using System.Collections.Generic;

namespace IronNestStats.Core.Models
{
    public sealed class MedalSnapshot
    {
        public MedalSnapshot(string name, double progress, IList<MedalTierSnapshot> tiers)
        {
            Name = name ?? "Medal";
            Progress = progress < 0 ? 0 : progress > 1 ? 1 : progress;
            Tiers = tiers ?? new List<MedalTierSnapshot>();
        }

        public string Name { get; }
        public double Progress { get; }
        public IList<MedalTierSnapshot> Tiers { get; }
    }
}
