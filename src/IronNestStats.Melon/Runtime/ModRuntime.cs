using System;
using IronNestStats.Core.Models;
using IronNestStats.Melon.Configuration;
using IronNestStats.Melon.Game;
using IronNestStats.Melon.UI;
using UnityEngine;

namespace IronNestStats.Melon.Runtime
{
    internal sealed class ModRuntime
    {
        private readonly ModSettings _settings;
        private readonly Action<string> _info;
        private readonly Action<string> _warning;
        private readonly IronNestGameFacade _game;
        private readonly OverlayRenderer _renderer;
        private StatsSnapshot _snapshot;
        private bool _showMedals;
        private bool _showStats;
        private float _nextRefresh;
        private float _nextErrorLog;
        private bool _active = true;
        private bool _lastMissionActive;
        private string _lastMissionName = string.Empty;

        public ModRuntime(ModSettings settings, Action<string> info, Action<string> warning)
        {
            _settings = settings;
            _info = info;
            _warning = warning;
            _game = new IronNestGameFacade();
            _renderer = new OverlayRenderer();
            _snapshot = StatsSnapshot.Inactive("English");
            _showMedals = settings.ShowMedalsByDefault.Value;
            _showStats = settings.ShowStatsByDefault.Value;
        }

        public void Update()
        {
            if (!_active) return;
            if (_settings.ToggleMedalsBinding.IsDown())
            {
                _showMedals = !_showMedals;
                _info("Medal panel: " + (_showMedals ? "shown" : "hidden"));
            }
            if (_settings.ToggleStatsBinding.IsDown())
            {
                _showStats = !_showStats;
                _info("Statistics panel: " + (_showStats ? "shown" : "hidden"));
            }
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + _settings.RefreshSeconds;

            try
            {
                _snapshot = _game.Capture(_settings.MedalLimit,
                    _settings.ShowMedalConditions.Value, _settings.ConditionLimit);
                if (_snapshot.Active != _lastMissionActive ||
                    (_snapshot.Active && _snapshot.MissionName != _lastMissionName))
                {
                    if (_snapshot.Active && !_lastMissionActive)
                    {
                        _showMedals = _settings.ShowMedalsByDefault.Value;
                        _showStats = _settings.ShowStatsByDefault.Value;
                    }
                    _lastMissionActive = _snapshot.Active;
                    _lastMissionName = _snapshot.MissionName;
                    _info(_snapshot.Active ? "Mission statistics active: " + _snapshot.MissionName : "Mission statistics inactive");
                }
            }
            catch (Exception exception)
            {
                _snapshot = StatsSnapshot.Inactive(_snapshot == null ? "English" : _snapshot.Language);
                if (Time.unscaledTime >= _nextErrorLog)
                {
                    _nextErrorLog = Time.unscaledTime + 5f;
                    _warning("Statistics capture failed: " + exception);
                }
            }
        }

        public void Draw()
        {
            if (!_active || _snapshot == null || !_snapshot.Active || (!_showMedals && !_showStats)) return;
            _renderer.Draw(_snapshot, _settings, _showMedals, _showStats, _game.Localization);
        }

        public void Shutdown()
        {
            _active = false;
        }
    }
}
