using System;
using System.Collections.Generic;

namespace IronNestStats.Core.Stats
{
    public static class StatsCalculator
    {
        public static void AddDerivedValues(IDictionary<string, double> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));

            var shots = Get(values, "ShotsFired");
            var hits = Get(values, "ShotsHit");
            var kills = Get(values, "Kills");
            var targets = Get(values, "TargetKills");
            var allies = Get(values, "AllyKills");
            var enemies = Get(values, "EnemyKills");
            var requisition = Get(values, "RequisitionPointsSpent");
            var missionSeconds = Get(values, "MissionTime");
            var recon = Get(values, "ReconUsed");
            var reconAfter = Get(values, "ReconUsedAfterFirstShot");
            var star = Get(values, "STARUsed");

            SetIfMissing(values, "ShotsMissed", Math.Max(0, shots - hits));
            SetIfMissing(values, "AccuracyPercent", Ratio(hits, shots) * 100.0);
            SetIfMissing(values, "MissPercent", Ratio(Get(values, "ShotsMissed"), shots) * 100.0);
            SetIfMissing(values, "KillsPerShot", Ratio(kills, shots));
            SetIfMissing(values, "KillsPerHit", Ratio(kills, hits));
            SetIfMissing(values, "TargetsPerShot", Ratio(targets, shots));
            SetIfMissing(values, "FriendlyFirePercent", Ratio(allies, kills) * 100.0);
            SetIfMissing(values, "EnemyKillPercent", Ratio(enemies, kills) * 100.0);
            SetIfMissing(values, "TargetKillPercent", Ratio(targets, kills) * 100.0);
            SetIfMissing(values, "MissionTimeMinutes", missionSeconds / 60.0);
            SetIfMissing(values, "ShotsPerMinute", Ratio(shots, missionSeconds / 60.0));
            SetIfMissing(values, "KillsPerMinute", Ratio(kills, missionSeconds / 60.0));
            SetIfMissing(values, "RequisitionPerKill", Ratio(requisition, kills));
            SetIfMissing(values, "RequisitionPerTarget", Ratio(requisition, targets));
            SetIfMissing(values, "ReconAfterFirstShotPercent", Ratio(reconAfter, recon) * 100.0);
            SetIfMissing(values, "STARUsagePercent", Ratio(star, shots) * 100.0);
            SetIfMissing(values, "PerfectAccuracy", shots > 0 && hits >= shots ? 1 : 0);
            SetIfMissing(values, "NoFriendlyFire", allies <= 0 ? 1 : 0);
            SetIfMissing(values, "NoReconUsed", recon <= 0 ? 1 : 0);
            SetIfMissing(values, "NoSTARUsed", star <= 0 ? 1 : 0);
        }

        public static double Get(IDictionary<string, double> values, string id)
        {
            double value;
            return values.TryGetValue(id, out value) && !double.IsNaN(value) && !double.IsInfinity(value) ? value : 0;
        }

        private static double Ratio(double numerator, double denominator)
        {
            return Math.Abs(denominator) < 0.000001 ? 0 : numerator / denominator;
        }

        private static void SetIfMissing(IDictionary<string, double> values, string id, double value)
        {
            if (!values.ContainsKey(id)) values[id] = value;
        }
    }
}
