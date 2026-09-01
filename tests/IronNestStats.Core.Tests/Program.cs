using System;
using System.Collections.Generic;
using IronNestStats.Core.Formatting;
using IronNestStats.Core.Stats;

internal static class Program
{
    private static int Main()
    {
        var values = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            { "Kills", 10 },
            { "TargetKills", 6 },
            { "EnemyKills", 7 },
            { "AllyKills", 1 },
            { "ShotsFired", 5 },
            { "ShotsHit", 4 },
            { "MissionTime", 120 },
            { "RequisitionPointsSpent", 50 },
            { "ReconUsed", 2 },
            { "ReconUsedAfterFirstShot", 1 },
            { "STARUsed", 1 }
        };

        StatsCalculator.AddDerivedValues(values);
        AssertNear(values["AccuracyPercent"], 80, "AccuracyPercent");
        AssertNear(values["ShotsMissed"], 1, "ShotsMissed");
        AssertNear(values["KillsPerShot"], 2, "KillsPerShot");
        AssertNear(values["KillsPerMinute"], 5, "KillsPerMinute");
        AssertNear(values["FriendlyFirePercent"], 10, "FriendlyFirePercent");
        AssertNear(values["RequisitionPerTarget"], 50.0 / 6.0, "RequisitionPerTarget");
        AssertEqual(ValueFormatter.Format(80, StatFormat.Percent, false), "80.0%", "percent formatting");
        AssertEqual(ValueFormatter.Format(125, StatFormat.Duration, false), "2:05", "duration formatting");

        var zeros = new Dictionary<string, double>();
        StatsCalculator.AddDerivedValues(zeros);
        AssertNear(zeros["AccuracyPercent"], 0, "zero denominator");
        Console.WriteLine("PASS: MissionStats.Core tests");
        return 0;
    }

    private static void AssertNear(double actual, double expected, string name)
    {
        if (Math.Abs(actual - expected) > 0.0001)
            throw new InvalidOperationException(name + ": expected " + expected + ", got " + actual);
    }

    private static void AssertEqual(string actual, string expected, string name)
    {
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
            throw new InvalidOperationException(name + ": expected " + expected + ", got " + actual);
    }
}
