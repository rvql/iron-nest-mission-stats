using System;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;

namespace IronNestStats.Melon.Configuration
{
    public sealed class ModSettings
    {
        public ModSettings()
        {
            var category = MelonPreferences.CreateCategory("MissionStats", "任务统计 / Mission Stats");
            category.SetFilePath(Path.Combine(MelonEnvironment.UserDataDirectory, "MissionStats.cfg"), true, false);

            ShowMedalsByDefault = category.CreateEntry("ShowMedalsByDefault", true,
                "默认显示奖章 / Show medals by default",
                "进入任务时显示奖章面板。 / Show the medal panel when a mission starts.");
            ShowStatsByDefault = category.CreateEntry("ShowStatsByDefault", false,
                "默认显示详细统计 / Show statistics by default",
                "进入任务时显示详细统计面板。 / Show the detailed statistics panel when a mission starts.");
            ToggleMedals = category.CreateEntry("ToggleMedals", "F7",
                "奖章面板快捷键 / Medal panel hotkey",
                "显示或隐藏奖章面板。 / Show or hide the medal panel.");
            ToggleStats = category.CreateEntry("ToggleStats", "F8",
                "详细统计快捷键 / Statistics panel hotkey",
                "显示或隐藏详细统计面板。 / Show or hide the detailed statistics panel.");

            MaxMedals = category.CreateEntry("MaxCurrentMedals", 4,
                "最大奖章数量 / Maximum medals",
                "最多显示的当前关卡奖章数量。 / Maximum number of current-mission medals shown.");
            ShowMedalConditions = category.CreateEntry("ShowConditions", true,
                "显示奖章条件 / Show medal conditions",
                "显示奖章条件及相关统计。 / Show medal conditions and their related statistics.");
            MaxConditionsPerMedal = category.CreateEntry("MaxConditionsPerMedal", 3,
                "每级最多条件数 / Maximum conditions per tier",
                "每个奖章等级最多读取的条件数。 / Maximum conditions read for each medal tier.");
            MedalPanelWidth = category.CreateEntry("MedalPanelWidth", 520f,
                "奖章面板最大宽度 / Maximum medal panel width",
                "奖章面板的最大像素宽度，实际宽度根据内容调整。 / Maximum medal panel width in pixels; actual width follows its content.");
            StatsPanelWidth = category.CreateEntry("StatsPanelWidth", 980f,
                "详细统计面板宽度 / Statistics panel width",
                "详细统计面板的像素宽度。 / Detailed statistics panel width in pixels.");
            PanelHeight = category.CreateEntry("PanelHeight", 650f,
                "面板最大高度 / Maximum panel height",
                "奖章和详细统计面板的最大像素高度。 / Maximum height of the medal and statistics panels in pixels.");
            BackgroundOpacity = category.CreateEntry("BackgroundOpacity", 0.78f,
                "背景不透明度 / Background opacity",
                "面板背景不透明度，范围为 0.0 到 1.0。 / Panel background opacity from 0.0 to 1.0.");
            RefreshInterval = category.CreateEntry("RefreshInterval", 0.2f,
                "刷新间隔 / Refresh interval",
                "统计数据刷新间隔，单位为秒。 / Statistics refresh interval in seconds.");
            category.SaveToFile(false);
        }

        public MelonPreferences_Entry<bool> ShowMedalsByDefault { get; }
        public MelonPreferences_Entry<bool> ShowStatsByDefault { get; }
        public MelonPreferences_Entry<string> ToggleMedals { get; }
        public MelonPreferences_Entry<string> ToggleStats { get; }
        public MelonPreferences_Entry<int> MaxMedals { get; }
        public MelonPreferences_Entry<bool> ShowMedalConditions { get; }
        public MelonPreferences_Entry<int> MaxConditionsPerMedal { get; }
        public MelonPreferences_Entry<float> MedalPanelWidth { get; }
        public MelonPreferences_Entry<float> StatsPanelWidth { get; }
        public MelonPreferences_Entry<float> PanelHeight { get; }
        public MelonPreferences_Entry<float> BackgroundOpacity { get; }
        public MelonPreferences_Entry<float> RefreshInterval { get; }

        public HotkeyBinding ToggleMedalsBinding => HotkeyBinding.Parse(ToggleMedals.Value, "F7");
        public HotkeyBinding ToggleStatsBinding => HotkeyBinding.Parse(ToggleStats.Value, "F8");
        public int MedalLimit => Math.Max(0, Math.Min(8, MaxMedals.Value));
        public int ConditionLimit => Math.Max(0, Math.Min(8, MaxConditionsPerMedal.Value));
        public float RefreshSeconds => Math.Max(0.1f, Math.Min(2f, RefreshInterval.Value));
        public float Opacity => Math.Max(0.05f, Math.Min(1f, BackgroundOpacity.Value));
    }
}
