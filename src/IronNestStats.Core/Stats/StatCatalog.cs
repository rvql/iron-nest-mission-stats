using System;
using System.Collections.Generic;
using System.Linq;

namespace IronNestStats.Core.Stats
{
    public static class StatCatalog
    {
        private static readonly StatDefinition[] Definitions =
        {
            D("MissionTime", "STR_MISSION_TIME", "Mission Time", "任务时间", StatFormat.Duration),
            D("CounterBatteryTimeRemaining", null, "Counter-battery Time", "反炮兵剩余时间", StatFormat.Duration),
            D("Kills", "STR_STAT_KILLS", "Kills", "击杀总数", StatFormat.Count),
            D("TargetKills", "STR_TARGETS_DESTROYED", "Target Kills", "目标击杀", StatFormat.Count),
            D("EnemyKills", "STR_STAT_ENEMY_KILLS", "Enemy Kills", "敌军击杀", StatFormat.Count),
            D("AllyKills", "STR_STAT_ALLIES_KILLED", "Allied Kills", "友军击杀", StatFormat.Count),
            D("StarsKilled", null, "Stars Killed", "星级目标击杀", StatFormat.Count),
            D("ShotsFiredAll", null, "All Shots", "所有射击", StatFormat.Count),
            D("ShotsFired", "STR_STAT_SHOTS_FIRED", "Shots Fired", "射击次数", StatFormat.Count),
            D("ShotsHit", null, "Shots Hit", "命中射击", StatFormat.Count),
            D("STARUsed", null, "STAR Used", "STAR 使用", StatFormat.Count),
            D("AverageImpactDistanceFromNearestTarget", null, "Average Impact Distance", "平均落点距离", StatFormat.Decimal),
            D("FirstShotTime", null, "First Shot Time", "首次射击时刻", StatFormat.Duration),
            D("LastTargetDestroyedTime", null, "Last Target Time", "最后目标摧毁时刻", StatFormat.Duration),
            D("RequisitionPointsSpent", "STR_STAT_REQUISITION_SPENT", "Requisition Spent", "征用点消耗", StatFormat.Decimal),
            D("ReconUsed", null, "Recon Used", "侦察使用", StatFormat.Count),
            D("ReconUsedAfterFirstShot", null, "Recon After First Shot", "首发后侦察", StatFormat.Count),
            D("LongestKillStreak", "STR_STAT_KILLSTREAK", "Longest Kill Streak", "最长连杀", StatFormat.Count),
            D("MostKillsBySingleImpact", "STR_STAT_MOST_KILLS_BY_IMPACT", "Most Kills by One Impact", "单次爆炸最多击杀", StatFormat.Count),
            D("BestThreeKillWindowSeconds", null, "Fastest Three Kills", "最快三杀窗口", StatFormat.Duration),
            D("AccuracyPercent", "STR_STAT_ACCURACY", "Accuracy", "命中率", StatFormat.Percent),
            D("MissPercent", null, "Miss Rate", "未命中率", StatFormat.Percent),
            D("ShotsMissed", "STR_MISSED_SHOTS", "Shots Missed", "未命中射击", StatFormat.Count),
            D("KillsPerShot", null, "Kills per Shot", "每发击杀", StatFormat.Decimal),
            D("KillsPerHit", "STR_STAT_KILLS_PER_HIT", "Kills per Hit", "每次命中击杀", StatFormat.Decimal),
            D("TargetsPerShot", null, "Targets per Shot", "每发目标击杀", StatFormat.Decimal),
            D("FriendlyFirePercent", null, "Friendly-fire Rate", "友军误伤率", StatFormat.Percent),
            D("EnemyKillPercent", null, "Enemy Kill Share", "敌军击杀占比", StatFormat.Percent),
            D("TargetKillPercent", null, "Target Kill Share", "目标击杀占比", StatFormat.Percent),
            D("AverageKillsPerImpact", null, "Average Kills per Impact", "平均每次爆炸击杀", StatFormat.Decimal),
            D("AverageStarsPerKill", null, "Average Stars per Kill", "平均每次击杀星级", StatFormat.Decimal),
            D("MissionTimeMinutes", null, "Mission Minutes", "任务分钟数", StatFormat.Decimal),
            D("TimeToFirstShot", null, "Time to First Shot", "首次开火用时", StatFormat.Duration),
            D("TimeToLastTargetKill", null, "Time to Last Target", "最后目标击杀用时", StatFormat.Duration),
            D("TimeFromFirstShotToLastTargetKill", null, "First Shot to Last Target", "首发至最后目标", StatFormat.Duration),
            D("ShotsPerMinute", null, "Shots per Minute", "每分钟射击", StatFormat.Decimal),
            D("KillsPerMinute", null, "Kills per Minute", "每分钟击杀", StatFormat.Decimal),
            D("RequisitionPerKill", null, "Requisition per Kill", "每次击杀消耗", StatFormat.Decimal),
            D("RequisitionPerTarget", null, "Requisition per Target", "每个目标消耗", StatFormat.Decimal),
            D("ReconAfterFirstShotPercent", null, "Recon after First Shot", "首发后侦察占比", StatFormat.Percent),
            D("STARUsagePercent", null, "STAR Usage", "STAR 使用率", StatFormat.Percent),
            D("PerfectAccuracy", null, "Perfect Accuracy", "完美命中", StatFormat.Boolean),
            D("NoFriendlyFire", null, "No Friendly Fire", "无友军误伤", StatFormat.Boolean),
            D("NoReconUsed", null, "No Recon Used", "未使用侦察", StatFormat.Boolean),
            D("NoSTARUsed", null, "No STAR Used", "未使用 STAR", StatFormat.Boolean),
            D("MultiKillShots", null, "Multi-kill Shots", "多杀射击", StatFormat.Count),
            D("TripleKillShots", null, "Triple-kill Shots", "三杀射击", StatFormat.Count),
            D("DirectHits", null, "Direct Hits", "直接命中", StatFormat.Count),
            D("MaxHitStreak", null, "Maximum Hit Streak", "最高连续命中", StatFormat.Count),
            D("CurrentRequisitionPoints", null, "Current Requisition", "当前征用点", StatFormat.Decimal)
        };

        private static readonly Dictionary<string, StatDefinition> ById =
            Definitions.ToDictionary(d => d.Id, StringComparer.OrdinalIgnoreCase);

        public static IEnumerable<StatDefinition> All => Definitions;

        public static StatDefinition Find(string id)
        {
            StatDefinition definition;
            return id != null && ById.TryGetValue(id, out definition) ? definition : null;
        }

        private static StatDefinition D(string id, string key, string en, string zh, StatFormat format)
        {
            return new StatDefinition(id, key, en, zh, format);
        }
    }
}
