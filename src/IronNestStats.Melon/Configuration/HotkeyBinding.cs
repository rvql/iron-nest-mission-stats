using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace IronNestStats.Melon.Configuration
{
    public sealed class HotkeyBinding
    {
        private readonly Key _mainKey;
        private readonly IList<Key> _modifiers;

        private HotkeyBinding(Key mainKey, IList<Key> modifiers)
        {
            _mainKey = mainKey;
            _modifiers = modifiers;
        }

        public bool IsDown()
        {
            var keyboard = Keyboard.current;
            if (_mainKey == Key.None || keyboard == null || !keyboard[_mainKey].wasPressedThisFrame) return false;
            for (var index = 0; index < _modifiers.Count; index++)
                if (!keyboard[_modifiers[index]].isPressed) return false;
            return true;
        }

        public static HotkeyBinding Parse(string configured, string fallback)
        {
            var source = string.IsNullOrWhiteSpace(configured) ? fallback : configured;
            var modifiers = new List<Key>();
            var main = Key.None;
            foreach (var raw in source.Split(new[] { '+', ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var token = raw.Trim();
                Key key;
                if (token.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || token.Equals("Control", StringComparison.OrdinalIgnoreCase))
                    key = Key.LeftCtrl;
                else if (token.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                    key = Key.LeftAlt;
                else if (token.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                    key = Key.LeftShift;
                else if (!Enum.TryParse(token, true, out key))
                    continue;

                if (key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt ||
                    key == Key.RightAlt || key == Key.LeftShift || key == Key.RightShift)
                    modifiers.Add(key);
                else
                    main = key;
            }
            return main == Key.None && !string.Equals(source, fallback, StringComparison.OrdinalIgnoreCase)
                ? Parse(fallback, fallback)
                : new HotkeyBinding(main, modifiers);
        }
    }
}
