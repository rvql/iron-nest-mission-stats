using System;
using System.Collections.Generic;
using System.Globalization;
using IronNestStats.Core.Models;
using IronNestStats.Core.Stats;

namespace IronNestStats.Melon.Game
{
    internal sealed class IronNestGameFacade
    {
        private static readonly string[] TrackedStatisticIds =
        {
            "Kills", "TargetKills", "EnemyKills", "AllyKills", "StarsKilled", "ShotsFired",
            "ShotsHit", "STARUsed", "AverageImpactDistanceFromNearestTarget", "FirstShotTime",
            "LastTargetDestroyedTime", "RequisitionPointsSpent", "ReconUsed", "ReconUsedAfterFirstShot",
            "LongestKillStreak", "MostKillsBySingleImpact", "BestThreeKillWindowSeconds",
            "CounterBatteryTimeRemaining", "MissionStartTime", "MissionCompleteTime", "MissionEndTime"
        };

        private static readonly IDictionary<string, string> TrackerMembers = new Dictionary<string, string>
        {
            { "MissionTime", "MissionTime_Mission" },
            { "ShotsFired", "ShotsFired_Mission" },
            { "TargetKills", "TargetsDestroyed_Mission" },
            { "ShotsHit", "HitsOnTargets_Mission" },
            { "ShotsMissed", "MissedShots_Mission" },
            { "AccuracyPercent", "Accuracy_Mission" },
            { "DirectHits", "DirectHits_Mission" },
            { "MaxHitStreak", "MaxHitStreak_Mission" },
            { "CurrentRequisitionPoints", "RequisitionPoints" }
        };

        private readonly ReflectionBridge _bridge = new ReflectionBridge();
        private readonly GameLocalizationService _localization;

        public IronNestGameFacade()
        {
            _localization = new GameLocalizationService(_bridge);
        }

        public GameLocalizationService Localization => _localization;

        public StatsSnapshot Capture(int maxMedals, bool includeConditions, int maxConditionsPerMedal)
        {
            var language = _localization.CurrentLanguage;
            var manager = _bridge.GetStatic("MissionManager", "Instance");
            if (manager == null || !IsMissionActive(_bridge.Get(manager, "CurrentPhase")))
                return StatsSnapshot.Inactive(language);

            var mission = _bridge.Get(manager, "CurrentMission");
            var state = _bridge.Get(manager, "CurrentMissionState");
            var trackedValues = _bridge.Get(state, "TrackingValues");
            var tracker = _bridge.GetStatic("MissionStatsTracker", "Instance");
            var missionName = _localization.ResolveIdentifier(_bridge.Get(mission, "MissionName"), "Mission");

            var statistics = ReadStatistics(trackedValues, tracker);
            var medals = ReadMedals(mission, trackedValues, maxMedals, includeConditions, maxConditionsPerMedal);
            return new StatsSnapshot(true, missionName, language, statistics, medals);
        }

        private IDictionary<string, double> ReadStatistics(object trackedValues, object tracker)
        {
            var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in TrackedStatisticIds) AddNumber(result, id, _bridge.Get(trackedValues, id));
            foreach (var pair in TrackerMembers) AddNumber(result, pair.Key, _bridge.Get(tracker, pair.Value));

            if (!result.ContainsKey("ShotsFiredAll") && result.ContainsKey("ShotsFired"))
                result["ShotsFiredAll"] = result["ShotsFired"];

            AddTimeDerivatives(result);
            StatsCalculator.AddDerivedValues(result);
            return result;
        }

        private IList<MedalSnapshot> ReadMedals(object mission, object trackedValues, int maxMedals,
            bool includeConditions, int maxConditionsPerMedal)
        {
            var result = new List<MedalSnapshot>();
            if (mission == null || trackedValues == null || maxMedals <= 0) return result;
            var categories = _bridge.Enumerate(_bridge.Get(mission, "Medals"));
            for (var index = 0; index < categories.Count && result.Count < maxMedals; index++)
            {
                var category = categories[index];
                var name = _localization.ResolveIdentifier(_bridge.Get(category, "displayNameV2"), "Medal " + (index + 1));
                var bronze = _bridge.Get(category, "BronzeConditions");
                var silver = _bridge.Get(category, "SilverConditions");
                var gold = _bridge.Get(category, "GoldConditions");
                var bronzeTier = CreateTierSnapshot("Bronze", bronze, trackedValues, includeConditions, maxConditionsPerMedal);
                var silverTier = CreateTierSnapshot("Silver", silver, trackedValues, includeConditions, maxConditionsPerMedal);
                var goldTier = CreateTierSnapshot("Gold", gold, trackedValues, includeConditions, maxConditionsPerMedal);
                var tiers = new List<MedalTierSnapshot> { bronzeTier, silverTier, goldTier };
                var progress = !bronzeTier.Achieved ? bronzeTier.Progress / 3.0
                    : !silverTier.Achieved ? (1.0 + silverTier.Progress) / 3.0
                    : !goldTier.Achieved ? (2.0 + goldTier.Progress) / 3.0
                    : 1.0;
                result.Add(new MedalSnapshot(name, progress, tiers));
            }
            return result;
        }

        private MedalTierSnapshot CreateTierSnapshot(string tier, object conditionSet, object trackedValues,
            bool includeConditions, int maxConditions)
        {
            var achieved = _bridge.Boolean(_bridge.Invoke(conditionSet, "Resolve", trackedValues));
            var allConditions = ReadConditions(conditionSet, trackedValues, int.MaxValue);
            var visibleConditions = new List<MedalConditionSnapshot>();
            if (includeConditions)
                for (var index = 0; index < allConditions.Count && index < maxConditions; index++)
                    visibleConditions.Add(allConditions[index]);
            return new MedalTierSnapshot(tier, achieved,
                achieved ? 1.0 : CalculateMedalProgress(allConditions), visibleConditions);
        }

        private IList<MedalConditionSnapshot> ReadConditions(object conditionSet, object trackedValues, int limit)
        {
            var result = new List<MedalConditionSnapshot>();
            var conditions = _bridge.Enumerate(_bridge.Get(conditionSet, "Conditions"));
            for (var index = 0; index < conditions.Count && result.Count < limit; index++)
            {
                var pair = conditions[index];
                var condition = _bridge.Get(pair, "Condition") ?? pair;
                var left = _bridge.Get(condition, "Left");
                var right = _bridge.Get(condition, "Right");
                var leftValue = _bridge.Number(_bridge.Invoke(left, "Resolve", trackedValues));
                var rightValue = _bridge.Number(_bridge.Invoke(right, "Resolve", trackedValues));
                var leftInfo = DescribeExpression(left, trackedValues);
                var rightInfo = DescribeExpression(right, trackedValues);
                var comparison = CompareText(_bridge.Text(_bridge.Get(condition, "Operator")));
                if (leftInfo.IsInline && !rightInfo.IsInline)
                {
                    result.Add(new MedalConditionSnapshot(rightInfo.Id ?? "Value", rightValue,
                        InvertComparison(comparison), leftValue, rightInfo.InputValues));
                }
                else
                {
                    result.Add(new MedalConditionSnapshot(leftInfo.Id ?? "Value", leftValue,
                        comparison, rightValue, leftInfo.InputValues));
                }
            }
            return result;
        }

        private ExpressionInfo DescribeExpression(object expression, object trackedValues)
        {
            var mode = _bridge.Text(_bridge.Get(expression, "Mode"));
            if (!string.IsNullOrEmpty(mode) && !mode.Equals("Value", StringComparison.OrdinalIgnoreCase))
            {
                var left = DescribeOperand(_bridge.Get(expression, "A"), trackedValues);
                var right = DescribeOperand(_bridge.Get(expression, "B"), trackedValues);
                var operation = MathOperatorText(_bridge.Text(_bridge.Get(expression, "MathOperator")));
                var inputs = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                MergeInputs(inputs, left.InputValues);
                MergeInputs(inputs, right.InputValues);
                return new ExpressionInfo((left.Id ?? "Value") + operation + (right.Id ?? "Value"), false, inputs);
            }
            var source = _bridge.Text(_bridge.Get(expression, "Source"));
            if (source.Equals("Variable", StringComparison.OrdinalIgnoreCase))
            {
                var id = _bridge.Text(_bridge.Get(expression, "Variable"));
                var value = _bridge.Number(_bridge.Invoke(expression, "Resolve", trackedValues));
                return ExpressionInfo.ForStatistic(id, value);
            }
            if (source.Equals("CustomVariable", StringComparison.OrdinalIgnoreCase))
            {
                var id = _bridge.Text(_bridge.Get(expression, "CustomVariableKey"));
                var value = _bridge.Number(_bridge.Invoke(expression, "Resolve", trackedValues));
                return ExpressionInfo.ForStatistic(id, value);
            }
            return new ExpressionInfo(null, true, new Dictionary<string, double>());
        }

        private ExpressionInfo DescribeOperand(object operand, object trackedValues)
        {
            var source = _bridge.Text(_bridge.Get(operand, "Source"));
            var value = _bridge.Number(_bridge.Invoke(operand, "Resolve", trackedValues));
            if (source.Equals("Variable", StringComparison.OrdinalIgnoreCase))
                return ExpressionInfo.ForStatistic(_bridge.Text(_bridge.Get(operand, "Variable")), value);
            if (source.Equals("CustomVariable", StringComparison.OrdinalIgnoreCase))
                return ExpressionInfo.ForStatistic(_bridge.Text(_bridge.Get(operand, "CustomVariableKey")), value);
            return new ExpressionInfo(value.ToString("0.##", CultureInfo.InvariantCulture), true,
                new Dictionary<string, double>());
        }

        private static void MergeInputs(IDictionary<string, double> target, IDictionary<string, double> source)
        {
            foreach (var pair in source) target[pair.Key] = pair.Value;
        }

        private static string MathOperatorText(string value)
        {
            return value == "Add" ? "+" : value == "Subtract" ? "-" :
                value == "Multiply" ? "*" : value == "Divide" ? "/" : "?";
        }

        private int ResolveTier(object bronze, object silver, object gold, object trackedValues)
        {
            if (_bridge.Boolean(_bridge.Invoke(gold, "Resolve", trackedValues))) return 3;
            if (_bridge.Boolean(_bridge.Invoke(silver, "Resolve", trackedValues))) return 2;
            if (_bridge.Boolean(_bridge.Invoke(bronze, "Resolve", trackedValues))) return 1;
            return 0;
        }

        private static double CalculateMedalProgress(IList<MedalConditionSnapshot> conditions)
        {
            if (conditions == null || conditions.Count == 0) return 0;
            var progress = 1.0;
            for (var index = 0; index < conditions.Count; index++)
                progress = Math.Min(progress, CalculateConditionProgress(conditions[index]));
            return Math.Max(0, Math.Min(1, progress));
        }

        private static double CalculateConditionProgress(MedalConditionSnapshot condition)
        {
            var current = condition.CurrentValue;
            var target = condition.TargetValue;
            switch (condition.Comparison)
            {
                case ">":
                case ">=":
                    if (current >= target) return 1;
                    return target <= 0 ? 0 : current / target;
                case "<":
                case "<=":
                    if (current <= target) return 1;
                    return current <= 0 ? 0 : target / current;
                case "=":
                    if (Math.Abs(current - target) < 0.0001) return 1;
                    var maximum = Math.Max(Math.Abs(current), Math.Abs(target));
                    return maximum <= 0 ? 0 : Math.Min(Math.Abs(current), Math.Abs(target)) / maximum;
                case "!=":
                    return Math.Abs(current - target) >= 0.0001 ? 1 : 0;
                default:
                    return 0;
            }
        }

        private static bool IsMissionActive(object phase)
        {
            if (phase == null) return false;
            var text = phase.ToString();
            if (string.Equals(text, "MissionActive", StringComparison.OrdinalIgnoreCase)) return true;
            int numeric;
            return int.TryParse(text, out numeric) && numeric == 2;
        }

        private void AddNumber(IDictionary<string, double> target, string id, object value)
        {
            if (value != null) target[id] = _bridge.Number(value);
        }

        private static void AddTimeDerivatives(IDictionary<string, double> values)
        {
            var start = StatsCalculator.Get(values, "MissionStartTime");
            var first = StatsCalculator.Get(values, "FirstShotTime");
            var last = StatsCalculator.Get(values, "LastTargetDestroyedTime");
            if (first > 0 && start > 0) values["TimeToFirstShot"] = Math.Max(0, first - start);
            if (last > 0 && start > 0) values["TimeToLastTargetKill"] = Math.Max(0, last - start);
            if (last > 0 && first > 0) values["TimeFromFirstShotToLastTargetKill"] = Math.Max(0, last - first);
        }

        private static string TierName(int tier)
        {
            return tier == 3 ? "Gold" : tier == 2 ? "Silver" : tier == 1 ? "Bronze" : "Unearned";
        }

        private static string CompareText(string value)
        {
            switch (value)
            {
                case "Equal": return "=";
                case "NotEqual": return "!=";
                case "GreaterThan": return ">";
                case "GreaterThanOrEqual": return ">=";
                case "LessThan": return "<";
                case "LessThanOrEqual": return "<=";
                default: return value ?? "?";
            }
        }

        private static string InvertComparison(string value)
        {
            return value == ">" ? "<" : value == ">=" ? "<=" : value == "<" ? ">" : value == "<=" ? ">=" : value;
        }

        private sealed class ExpressionInfo
        {
            public ExpressionInfo(string id, bool isInline, IDictionary<string, double> inputValues)
            {
                Id = id;
                IsInline = isInline;
                InputValues = inputValues;
            }

            public string Id { get; }
            public bool IsInline { get; }
            public IDictionary<string, double> InputValues { get; }

            public static ExpressionInfo ForStatistic(string id, double value)
            {
                var inputs = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrEmpty(id)) inputs[id] = value;
                return new ExpressionInfo(id, false, inputs);
            }
        }
    }
}
