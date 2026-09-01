using System;
using System.Collections.Generic;

namespace MelonLoader
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class MelonInfoAttribute : Attribute
    {
        public MelonInfoAttribute(Type type, string name, string version, string author, string downloadLink = null) { }
    }

    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class MelonGameAttribute : Attribute
    {
        public MelonGameAttribute(string developer, string name) { }
    }

    public abstract class MelonMod
    {
        protected MelonMod() { LoggerInstance = new MelonLogger.Instance(); }
        public MelonLogger.Instance LoggerInstance { get; }
        public virtual void OnInitializeMelon() { }
        public virtual void OnUpdate() { }
        public virtual void OnGUI() { }
        public virtual void OnDeinitializeMelon() { }
    }

    public static class MelonLogger
    {
        public sealed class Instance
        {
            public void Msg(object value) { }
            public void Warning(object value) { }
            public void Error(object value) { }
        }
    }

    public static class MelonPreferences
    {
        public static MelonPreferences_Category CreateCategory(string identifier, string displayName = null)
        {
            return new MelonPreferences_Category();
        }
    }

    public sealed class MelonPreferences_Category
    {
        public MelonPreferences_Entry<T> CreateEntry<T>(string identifier, T defaultValue,
            string displayName = null, string description = null, bool isHidden = false,
            bool dontSaveDefault = false, object validator = null, string oldIdentifier = null)
        {
            return new MelonPreferences_Entry<T> { Value = defaultValue };
        }

        public void SaveToFile(bool printmsg = true) { }
        public void SetFilePath(string path, bool autoload, bool printmsg) { }
    }

    public sealed class MelonPreferences_Entry<T>
    {
        public T Value { get; set; }
    }
}

namespace MelonLoader.Utils
{
    public static class MelonEnvironment
    {
        public static string UserDataDirectory { get { return "."; } }
    }
}

namespace UnityEngine
{
    public enum FontStyle { Normal, Bold }
    public enum TextAnchor { MiddleLeft, MiddleRight, MiddleCenter }

    public struct Color
    {
        public Color(float r, float g, float b, float a) { }
        public static Color white { get { return new Color(); } }
    }

    public struct Rect
    {
        public Rect(float x, float y, float width, float height)
        {
            this.x = x; this.y = y; this.width = width; this.height = height;
        }
        public float x, y, width, height;
        public float right { get { return x + width; } }
        public float bottom { get { return y + height; } }
        public bool Contains(Vector2 point) { return true; }
    }

    public struct Vector2
    {
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public float x, y;
    }
    public sealed class Event
    {
        public static Event current { get { return new Event(); } }
        public Vector2 mousePosition { get { return new Vector2(); } }
    }

    public sealed class GUIStyleState { public Color textColor { get; set; } }
    public sealed class GUIStyle
    {
        public GUIStyle() { normal = new GUIStyleState(); }
        public GUIStyle(GUIStyle other) { normal = new GUIStyleState(); }
        public int fontSize { get; set; }
        public FontStyle fontStyle { get; set; }
        public TextAnchor alignment { get; set; }
        public bool wordWrap { get; set; }
        public GUIStyleState normal { get; }
        public Vector2 CalcSize(GUIContent content) { return new Vector2(); }
        public float CalcHeight(GUIContent content, float width) { return 20f; }
    }

    public sealed class GUISkin
    {
        public GUISkin() { box = new GUIStyle(); label = new GUIStyle(); }
        public GUIStyle box { get; }
        public GUIStyle label { get; }
    }

    public sealed class GUIContent
    {
        public GUIContent() { }
        public GUIContent(string text) { }
        public static GUIContent none { get { return new GUIContent(); } }
    }
    public class Texture { }
    public sealed class Texture2D : Texture { public static Texture2D whiteTexture { get { return new Texture2D(); } } }
    public static class GUI
    {
        public static int depth { get; set; }
        public static Color color { get; set; }
        public static GUISkin skin { get { return new GUISkin(); } }
        public static void Box(Rect rect, GUIContent content, GUIStyle style) { }
        public static void Label(Rect rect, string text, GUIStyle style) { }
        public static void DrawTexture(Rect rect, Texture texture) { }
    }
    public static class Screen { public static int width { get { return 1920; } } public static int height { get { return 1080; } } }
    public static class Time { public static float unscaledTime { get { return 0; } } }
}

namespace UnityEngine.InputSystem
{
    public enum Key { None, F7, F8, LeftCtrl, RightCtrl, LeftAlt, RightAlt, LeftShift, RightShift, Insert }

    public sealed class KeyControl
    {
        public bool wasPressedThisFrame { get { return false; } }
        public bool isPressed { get { return false; } }
    }

    public sealed class Keyboard
    {
        public static Keyboard current { get { return new Keyboard(); } }
        public KeyControl this[Key key] { get { return new KeyControl(); } }
    }

    public sealed class Vector2Control
    {
        public UnityEngine.Vector2 ReadValue() { return new UnityEngine.Vector2(); }
    }

    public sealed class Mouse
    {
        public static Mouse current { get { return new Mouse(); } }
        public Vector2Control position { get { return new Vector2Control(); } }
    }
}
