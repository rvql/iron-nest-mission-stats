using System;
using System.Collections.Generic;
using System.Globalization;
using IronNestStats.Core.Formatting;
using IronNestStats.Core.Models;
using IronNestStats.Core.Stats;
using IronNestStats.Melon.Configuration;
using IronNestStats.Melon.Game;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IronNestStats.Melon.UI
{
    internal sealed class OverlayRenderer
    {
        private const float ProgressBarWidth = 240f;
        private GUIStyle _panelTitle;
        private GUIStyle _missionTitle;
        private GUIStyle _medalName;
        private GUIStyle _conditionText;
        private GUIStyle _relatedStats;
        private GUIStyle _tableHeader;
        private GUIStyle _conditionValue;
        private GUIStyle _statLabel;
        private GUIStyle _statValue;
        private GUIStyle _tickLabel;

        public void Draw(StatsSnapshot snapshot, ModSettings settings, bool showMedals, bool showStats,
            GameLocalizationService localization)
        {
            EnsureStyles();
            MedalPanelLayout medalLayout = null;
            if (showMedals)
            {
                medalLayout = CreateMedalPanelLayout(snapshot, settings, localization);
                DrawMedalPanel(snapshot, settings, localization, medalLayout);
            }
            if (showStats) DrawStatisticsPanel(snapshot, settings, localization,
                medalLayout == null ? 0f : medalLayout.PanelWidth);
        }

        private void DrawMedalPanel(StatsSnapshot snapshot, ModSettings settings,
            GameLocalizationService localization, MedalPanelLayout layout)
        {
            var chinese = localization.IsChinese(snapshot.Language);
            var width = layout.PanelWidth;
            var maximumHeight = Math.Min(Math.Max(320f, settings.PanelHeight.Value), Screen.height - 32f);
            var height = Math.Min(Math.Max(220f, layout.ContentHeight), maximumHeight);
            var panel = new Rect(Screen.width - width - 16f, 16f, width, height);
            if (IsPointerInside(panel)) DrawBackground(panel, settings.Opacity);

            GUI.Label(new Rect(panel.x + 16f, panel.y + 10f, panel.width - 32f, 20f),
                chinese ? "任务" : "MISSION", _panelTitle);
            GUI.Label(new Rect(panel.x + 16f, panel.y + 30f, panel.width - 32f, layout.MissionTitleHeight),
                snapshot.MissionName, _missionTitle);

            var y = panel.y + layout.HeaderHeight;
            MedalSnapshot hoveredMedal = null;
            for (var medalIndex = 0; medalIndex < snapshot.Medals.Count; medalIndex++)
            {
                var medal = snapshot.Medals[medalIndex];
                var medalTop = y;
                var titleHeight = layout.MedalTitleHeights[medalIndex];
                GUI.Label(new Rect(panel.x + 16f, y, panel.width - 32f, titleHeight), medal.Name, _medalName);
                y += titleHeight + 2f;

                var bar = new Rect(panel.right - 16f - ProgressBarWidth, y, ProgressBarWidth, 18f);
                DrawSolid(bar, new Color(0.13f, 0.16f, 0.20f, 0.95f));
                var fill = new Rect(bar.x + 2f, bar.y + 2f,
                    Math.Max(0f, (bar.width - 4f) * (float)medal.Progress), bar.height - 4f);
                DrawSolid(fill, ProgressColor(medal.Progress));
                DrawTierTicks(bar, snapshot.Language, localization);
                y += 21f;

                var related = BuildRelatedStatistics(medal, snapshot.Language, localization);
                var relatedHeight = layout.RelatedStatisticHeights[medalIndex];
                GUI.Label(new Rect(panel.x + 16f, y, panel.width - 32f, relatedHeight), related, _relatedStats);
                y += relatedHeight + 2f;
                y += 7f;

                var triggerArea = new Rect(panel.x + 16f, medalTop, panel.width - 32f, y - medalTop);
                if (IsPointerInside(triggerArea)) hoveredMedal = medal;

                if (y > panel.bottom - 20f) break;
            }

            if (hoveredMedal != null && GetConditionRowCount(hoveredMedal) > 0 && y < panel.bottom - 40f)
            {
                GUI.Label(new Rect(panel.x + 16f, y, panel.width - 32f, 22f),
                    hoveredMedal.Name, _medalName);
                DrawConditionTable(panel, y + 24f, hoveredMedal, snapshot.Language, localization,
                    layout.ConditionTable);
            }
        }

        private MedalPanelLayout CreateMedalPanelLayout(StatsSnapshot snapshot, ModSettings settings,
            GameLocalizationService localization)
        {
            var maximumPanelWidth = Math.Min(Math.Max(380f, settings.MedalPanelWidth.Value), Screen.width - 32f);
            var maximumContentWidth = maximumPanelWidth - 32f;
            var table = CreateConditionTableLayout(snapshot.Medals, snapshot.Language, localization,
                ProgressBarWidth, maximumContentWidth);

            var requiredContentWidth = Math.Max(ProgressBarWidth, table.TotalWidth);
            requiredContentWidth = Math.Max(requiredContentWidth,
                MeasureWidth(_missionTitle, snapshot.MissionName) + 4f);
            foreach (var medal in snapshot.Medals)
                requiredContentWidth = Math.Max(requiredContentWidth,
                    MeasureWidth(_medalName, medal.Name) + 4f);
            requiredContentWidth = Math.Min(requiredContentWidth, maximumContentWidth);

            var panelWidth = requiredContentWidth + 32f;
            var missionHeight = Math.Max(32f,
                MeasureHeight(_missionTitle, snapshot.MissionName, requiredContentWidth));
            var headerHeight = 30f + missionHeight + 8f;
            var medalTitleHeights = new List<float>();
            var relatedHeights = new List<float>();
            var summariesHeight = 0f;
            foreach (var medal in snapshot.Medals)
            {
                var titleHeight = Math.Max(22f,
                    MeasureHeight(_medalName, medal.Name, requiredContentWidth));
                var related = BuildRelatedStatistics(medal, snapshot.Language, localization);
                var relatedHeight = Math.Max(20f,
                    MeasureHeight(_relatedStats, related, requiredContentWidth));
                medalTitleHeights.Add(titleHeight);
                relatedHeights.Add(relatedHeight);
                summariesHeight += titleHeight + relatedHeight + 32f;
            }

            var detailRows = GetMaximumConditionRows(snapshot.Medals);
            var detailHeight = detailRows == 0 ? 0f : 56f + detailRows * 34f;
            return new MedalPanelLayout(panelWidth, headerHeight, missionHeight,
                headerHeight + summariesHeight + detailHeight, medalTitleHeights, relatedHeights, table);
        }

        private ConditionTableLayout CreateConditionTableLayout(IList<MedalSnapshot> medals, string language,
            GameLocalizationService localization, float minimumWidth, float maximumWidth)
        {
            var labelWidth = 120f;
            var tierWidths = new[] { 40f, 40f, 40f };
            for (var tierIndex = 0; tierIndex < 3; tierIndex++)
            {
                var tierName = tierIndex == 0 ? "Bronze" : tierIndex == 1 ? "Silver" : "Gold";
                tierWidths[tierIndex] = Math.Max(tierWidths[tierIndex],
                    MeasureWidth(_tableHeader, localization.GetTierLabel(tierName, language)) + 16f);
            }

            foreach (var medal in medals)
            {
                var rows = BuildConditionRows(medal, language, localization);
                foreach (var row in rows)
                {
                    labelWidth = Math.Max(labelWidth, MeasureWidth(_conditionText, row.Label) + 18f);
                    for (var tierIndex = 0; tierIndex < 3; tierIndex++)
                        tierWidths[tierIndex] = Math.Max(tierWidths[tierIndex],
                            MeasureWidth(_conditionValue, row.TargetValues[tierIndex]) + 16f);
                }
            }

            var tierTotal = tierWidths[0] + tierWidths[1] + tierWidths[2];
            var total = labelWidth + tierTotal;
            if (total > maximumWidth)
            {
                labelWidth = Math.Max(120f, maximumWidth - tierTotal);
                total = labelWidth + tierTotal;
                if (total > maximumWidth)
                {
                    var availableTierWidth = Math.Max(40f, (maximumWidth - 120f) / 3f);
                    for (var tierIndex = 0; tierIndex < 3; tierIndex++)
                        tierWidths[tierIndex] = Math.Min(tierWidths[tierIndex], availableTierWidth);
                    tierTotal = tierWidths[0] + tierWidths[1] + tierWidths[2];
                    labelWidth = Math.Max(120f, maximumWidth - tierTotal);
                    total = labelWidth + tierTotal;
                }
            }
            if (total < minimumWidth)
            {
                labelWidth += minimumWidth - total;
                total = minimumWidth;
            }
            return new ConditionTableLayout(labelWidth, tierWidths, Math.Min(total, maximumWidth));
        }

        private static float MeasureWidth(GUIStyle style, string value)
        {
            if (string.IsNullOrEmpty(value)) return 0f;
            return style.CalcSize(new GUIContent(value)).x;
        }

        private static float MeasureHeight(GUIStyle style, string value, float width)
        {
            if (string.IsNullOrEmpty(value)) return 0f;
            return style.CalcHeight(new GUIContent(value), width);
        }

        private string BuildRelatedStatistics(MedalSnapshot medal, string language, GameLocalizationService localization)
        {
            var values = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var tier in medal.Tiers)
                foreach (var condition in tier.Conditions)
                {
                    if (condition.InputValues.Count > 0)
                        foreach (var input in condition.InputValues) values[input.Key] = input.Value;
                    else
                        values[condition.StatisticId] = condition.CurrentValue;
                }

            var parts = new List<string>();
            foreach (var pair in values)
                parts.Add(localization.GetStatLabel(pair.Key, language) + " " +
                          pair.Value.ToString("0.##", CultureInfo.InvariantCulture));
            return string.Join(" · ", parts);
        }

        private float DrawConditionTable(Rect panel, float y, MedalSnapshot medal,
            string language, GameLocalizationService localization, ConditionTableLayout layout)
        {
            const float headerHeight = 22f;
            const float rowHeight = 34f;
            var rows = BuildConditionRows(medal, language, localization);
            if (rows.Count == 0) return y;

            var width = layout.TotalWidth;
            var x = panel.right - 16f - width;
            var labelWidth = layout.LabelWidth;
            var tableHeight = headerHeight + rows.Count * rowHeight;
            var lineColor = new Color(0.40f, 0.52f, 0.60f, 0.55f);

            DrawSolid(new Rect(x, y, width, 1f), lineColor);
            DrawSolid(new Rect(x, y + headerHeight, width, 1f), lineColor);
            DrawSolid(new Rect(x, y, 1f, tableHeight), lineColor);
            DrawSolid(new Rect(x + labelWidth, y, 1f, tableHeight), lineColor);
            for (var column = 1; column <= 3; column++)
                DrawSolid(new Rect(x + labelWidth + SumTierWidths(layout, column), y, 1f, tableHeight), lineColor);

            for (var column = 0; column < 3; column++)
            {
                var tierName = column == 0 ? "Bronze" : column == 1 ? "Silver" : "Gold";
                var columnX = x + labelWidth + SumTierWidths(layout, column);
                GUI.Label(new Rect(columnX + 5f, y + 1f,
                        layout.TierWidths[column] - 10f, headerHeight - 2f),
                    localization.GetTierLabel(tierName, language), _tableHeader);
            }

            for (var row = 0; row < rows.Count; row++)
            {
                var rowY = y + headerHeight + row * rowHeight;
                DrawSolid(new Rect(x, rowY + rowHeight, width, 1f), lineColor);
                GUI.Label(new Rect(x + 7f, rowY + 2f, labelWidth - 14f, rowHeight - 4f),
                    rows[row].Label, _conditionText);
                for (var column = 0; column < 3; column++)
                {
                    var columnX = x + labelWidth + SumTierWidths(layout, column);
                    GUI.Label(new Rect(columnX + 5f, rowY + 2f,
                            layout.TierWidths[column] - 10f, rowHeight - 4f),
                        rows[row].TargetValues[column] ?? string.Empty, _conditionValue);
                }
            }

            return y + tableHeight + 10f;
        }

        private static MedalTierSnapshot FindTier(MedalSnapshot medal, string tierName)
        {
            foreach (var tier in medal.Tiers)
                if (string.Equals(tier.Tier, tierName, StringComparison.OrdinalIgnoreCase)) return tier;
            return null;
        }

        private static IList<ConditionTableRow> BuildConditionRows(MedalSnapshot medal, string language,
            GameLocalizationService localization)
        {
            var rows = new List<ConditionTableRow>();
            var rowsByKey = new Dictionary<string, ConditionTableRow>(StringComparer.OrdinalIgnoreCase);
            for (var tierIndex = 0; tierIndex < 3; tierIndex++)
            {
                var tierName = tierIndex == 0 ? "Bronze" : tierIndex == 1 ? "Silver" : "Gold";
                var tier = FindTier(medal, tierName);
                if (tier == null) continue;
                var occurrences = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var condition in tier.Conditions)
                {
                    var baseKey = GetConditionBaseKey(condition);
                    int occurrence;
                    occurrences.TryGetValue(baseKey, out occurrence);
                    occurrences[baseKey] = occurrence + 1;
                    var key = baseKey + "\u001f" + occurrence.ToString(CultureInfo.InvariantCulture);
                    ConditionTableRow row;
                    if (!rowsByKey.TryGetValue(key, out row))
                    {
                        var label = localization.GetStatLabel(condition.StatisticId, language) + " " +
                                    DisplayComparison(condition.Comparison);
                        row = new ConditionTableRow(label);
                        rowsByKey[key] = row;
                        rows.Add(row);
                    }
                    row.TargetValues[tierIndex] = FormatConditionNumber(condition.TargetValue);
                }
            }
            return rows;
        }

        private static int GetConditionRowCount(MedalSnapshot medal)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var tier in medal.Tiers)
            {
                var occurrences = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var condition in tier.Conditions)
                {
                    var baseKey = GetConditionBaseKey(condition);
                    int occurrence;
                    occurrences.TryGetValue(baseKey, out occurrence);
                    occurrences[baseKey] = occurrence + 1;
                    keys.Add(baseKey + "\u001f" + occurrence.ToString(CultureInfo.InvariantCulture));
                }
            }
            return keys.Count;
        }

        private static string GetConditionBaseKey(MedalConditionSnapshot condition)
        {
            return (condition.StatisticId ?? "Value") + "\u001f" + (condition.Comparison ?? "?");
        }

        private static int GetMaximumConditionRows(IList<MedalSnapshot> medals)
        {
            var result = 0;
            foreach (var medal in medals) result = Math.Max(result, GetConditionRowCount(medal));
            return result;
        }

        private static float SumTierWidths(ConditionTableLayout layout, int exclusiveEnd)
        {
            var result = 0f;
            for (var index = 0; index < exclusiveEnd; index++) result += layout.TierWidths[index];
            return result;
        }

        private static string DisplayComparison(string comparison)
        {
            return comparison == ">=" ? "≥" : comparison == "<=" ? "≤" :
                comparison == "!=" ? "≠" : comparison ?? "?";
        }

        private static string FormatConditionNumber(double value)
        {
            if (double.IsNaN(value)) return "—";
            if (double.IsPositiveInfinity(value)) return "∞";
            if (double.IsNegativeInfinity(value)) return "-∞";
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private void DrawStatisticsPanel(StatsSnapshot snapshot, ModSettings settings,
            GameLocalizationService localization, float medalPanelWidth)
        {
            var chinese = localization.IsChinese(snapshot.Language);
            var reserved = medalPanelWidth > 0f ? medalPanelWidth + 32f : 0f;
            var width = Math.Min(Math.Max(640f, settings.StatsPanelWidth.Value), Screen.width - reserved - 32f);
            if (width < 540f) return;
            var height = Math.Min(Math.Max(260f, settings.PanelHeight.Value), Screen.height - 32f);
            var panel = new Rect(16f, 16f, width, height);
            DrawBackground(panel, settings.Opacity);

            GUI.Label(new Rect(panel.x + 18f, panel.y + 10f, panel.width - 36f, 26f),
                chinese ? "详细统计" : "DETAILED STATISTICS", _panelTitle);
            GUI.Label(new Rect(panel.x + 18f, panel.y + 36f, panel.width - 36f, 22f),
                snapshot.MissionName, _statLabel);

            var rows = new List<StatRow>();
            foreach (var definition in StatCatalog.All)
            {
                double value;
                if (!snapshot.Statistics.TryGetValue(definition.Id, out value)) continue;
                rows.Add(new StatRow(localization.GetStatLabel(definition.Id, snapshot.Language),
                    ValueFormatter.Format(value, definition.Format, chinese)));
            }
            DrawStatRows(panel, rows);
        }

        private void DrawStatRows(Rect panel, IList<StatRow> rows)
        {
            const float top = 64f;
            const float lineHeight = 21f;
            const float gap = 16f;
            var availableHeight = panel.height - top - 14f;
            var rowsPerColumn = Math.Max(1, (int)(availableHeight / lineHeight));
            var columnCount = Math.Min(3, Math.Max(2, (int)Math.Ceiling(rows.Count / (double)rowsPerColumn)));
            var columnWidth = (panel.width - 36f - gap * (columnCount - 1)) / columnCount;
            for (var index = 0; index < rows.Count && index < rowsPerColumn * columnCount; index++)
            {
                var column = index / rowsPerColumn;
                var row = index % rowsPerColumn;
                var x = panel.x + 18f + column * (columnWidth + gap);
                var y = panel.y + top + row * lineHeight;
                var labelWidth = columnWidth * 0.7f;
                GUI.Label(new Rect(x, y, labelWidth, lineHeight), rows[index].Label, _statLabel);
                GUI.Label(new Rect(x + labelWidth, y, columnWidth - labelWidth, lineHeight), rows[index].Value, _statValue);
            }
        }

        private static void DrawBackground(Rect rect, float opacity)
        {
            DrawSolid(rect, new Color(0.025f, 0.035f, 0.05f, opacity));
            DrawSolid(new Rect(rect.x, rect.y, 3f, rect.height), new Color(0.22f, 0.62f, 0.86f, Math.Min(1f, opacity + 0.15f)));
        }

        private static bool IsPointerInside(Rect rect)
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                var screenPosition = mouse.position.ReadValue();
                var guiPosition = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
                return rect.Contains(guiPosition);
            }
            var currentEvent = Event.current;
            return currentEvent != null && rect.Contains(currentEvent.mousePosition);
        }

        private static void DrawSolid(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private static Color ProgressColor(double progress)
        {
            if (progress >= 1) return new Color(0.88f, 0.69f, 0.20f, 1f);
            if (progress >= 0.66) return new Color(0.28f, 0.72f, 0.45f, 1f);
            return new Color(0.20f, 0.55f, 0.82f, 1f);
        }

        private void DrawTierTicks(Rect bar, string language, GameLocalizationService localization)
        {
            for (var index = 0; index < 3; index++)
            {
                var position = (index + 1f) / 3f;
                var x = bar.x + 2f + (bar.width - 4f) * position;
                if (index == 2) x = bar.right - 2f;
                var color = index == 0 ? new Color(0.78f, 0.43f, 0.18f, 1f) :
                    index == 1 ? new Color(0.76f, 0.80f, 0.84f, 1f) :
                    new Color(0.96f, 0.76f, 0.20f, 1f);
                DrawSolid(new Rect(x - 1f, bar.y + 1f, 2f, bar.height - 2f), color);
                var tierName = index == 0 ? "Bronze" : index == 1 ? "Silver" : "Gold";
                var fullLabel = localization.GetTierLabel(tierName, language);
                var label = string.IsNullOrEmpty(fullLabel) ? tierName.Substring(0, 1) : fullLabel.Substring(0, 1);
                GUI.Label(new Rect(x - 22f, bar.y, 18f, bar.height), label, _tickLabel);
            }
        }

        private void EnsureStyles()
        {
            if (_panelTitle != null) return;
            _panelTitle = Style(13, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.52f, 0.74f, 0.88f, 1f));
            _missionTitle = Style(19, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.96f, 0.97f, 0.99f, 1f));
            _missionTitle.wordWrap = true;
            _medalName = Style(15, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.93f, 0.94f, 0.96f, 1f));
            _medalName.wordWrap = true;
            _conditionText = Style(10, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.68f, 0.74f, 0.79f, 1f));
            _conditionText.wordWrap = true;
            _relatedStats = Style(10, FontStyle.Normal, TextAnchor.MiddleRight, new Color(0.70f, 0.78f, 0.84f, 1f));
            _relatedStats.wordWrap = true;
            _tableHeader = Style(11, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.88f, 0.91f, 0.94f, 1f));
            _conditionValue = Style(11, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.96f, 0.82f, 0.38f, 1f));
            _statLabel = Style(13, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.76f, 0.82f, 0.87f, 1f));
            _statValue = Style(13, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.96f, 0.82f, 0.38f, 1f));
            _tickLabel = Style(9, FontStyle.Bold, TextAnchor.MiddleRight, Color.white);
        }

        private static GUIStyle Style(int size, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            var style = new GUIStyle { fontSize = size, fontStyle = fontStyle, alignment = alignment };
            style.normal.textColor = color;
            return style;
        }

        private sealed class MedalPanelLayout
        {
            public MedalPanelLayout(float panelWidth, float headerHeight, float missionTitleHeight,
                float contentHeight, IList<float> medalTitleHeights, IList<float> relatedStatisticHeights,
                ConditionTableLayout conditionTable)
            {
                PanelWidth = panelWidth;
                HeaderHeight = headerHeight;
                MissionTitleHeight = missionTitleHeight;
                ContentHeight = contentHeight;
                MedalTitleHeights = medalTitleHeights;
                RelatedStatisticHeights = relatedStatisticHeights;
                ConditionTable = conditionTable;
            }

            public float PanelWidth { get; }
            public float HeaderHeight { get; }
            public float MissionTitleHeight { get; }
            public float ContentHeight { get; }
            public IList<float> MedalTitleHeights { get; }
            public IList<float> RelatedStatisticHeights { get; }
            public ConditionTableLayout ConditionTable { get; }
        }

        private sealed class ConditionTableLayout
        {
            public ConditionTableLayout(float labelWidth, float[] tierWidths, float totalWidth)
            {
                LabelWidth = labelWidth;
                TierWidths = tierWidths;
                TotalWidth = totalWidth;
            }

            public float LabelWidth { get; }
            public float[] TierWidths { get; }
            public float TotalWidth { get; }
        }

        private sealed class ConditionTableRow
        {
            public ConditionTableRow(string label)
            {
                Label = label;
                TargetValues = new string[3];
            }

            public string Label { get; }
            public string[] TargetValues { get; }
        }

        private sealed class StatRow
        {
            public StatRow(string label, string value) { Label = label; Value = value; }
            public string Label { get; }
            public string Value { get; }
        }
    }
}
