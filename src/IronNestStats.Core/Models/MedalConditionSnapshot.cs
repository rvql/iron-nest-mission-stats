using System.Collections.Generic;

namespace IronNestStats.Core.Models
{
    public sealed class MedalConditionSnapshot
    {
        public MedalConditionSnapshot(string statisticId, double currentValue, string comparison, double targetValue,
            IDictionary<string, double> inputValues)
        {
            StatisticId = statisticId ?? "Value";
            CurrentValue = currentValue;
            Comparison = comparison ?? "?";
            TargetValue = targetValue;
            InputValues = inputValues ?? new Dictionary<string, double>();
        }

        public string StatisticId { get; }
        public double CurrentValue { get; }
        public string Comparison { get; }
        public double TargetValue { get; }
        public IDictionary<string, double> InputValues { get; }
    }
}
