using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IronNestStats.Melon.Game
{
    internal sealed class ReflectionBridge
    {
        private readonly Dictionary<string, Type> _types = new Dictionary<string, Type>(StringComparer.Ordinal);
        private readonly Dictionary<string, MemberInfo> _members = new Dictionary<string, MemberInfo>(StringComparer.Ordinal);

        public Type FindType(string fullName)
        {
            Type cached;
            if (_types.TryGetValue(fullName, out cached)) return cached;
            var candidates = new List<string> { fullName };
            var separator = fullName.LastIndexOf('.');
            if (separator < 0)
                candidates.Add("Il2Cpp." + fullName);
            else
                candidates.Add("Il2Cpp" + fullName);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var candidate in candidates)
                {
                    var type = assembly.GetType(candidate, false);
                    if (type == null) continue;
                    _types[fullName] = type;
                    return type;
                }
            }
            return null;
        }

        public object GetStatic(string typeName, string memberName)
        {
            var type = FindType(typeName);
            return type == null ? null : GetValue(type, null, memberName, BindingFlags.Static);
        }

        public object Get(object instance, string memberName)
        {
            return instance == null ? null : GetValue(instance.GetType(), instance, memberName, BindingFlags.Instance);
        }

        public object Invoke(object instance, string methodName, params object[] arguments)
        {
            if (instance == null) return null;
            var methods = instance.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(method => method.Name == methodName && method.GetParameters().Length == arguments.Length);
            foreach (var method in methods)
            {
                try
                {
                    return method.Invoke(instance, AdaptArguments(method.GetParameters(), arguments));
                }
                catch (ArgumentException)
                {
                    // Try the next overload.
                }
            }
            return null;
        }

        public IList<object> Enumerate(object collection)
        {
            var result = new List<object>();
            if (collection == null) return result;
            var enumerable = collection as IEnumerable;
            if (enumerable != null)
            {
                foreach (var item in enumerable) if (item != null) result.Add(item);
                return result;
            }

            var countObject = Get(collection, "Count") ?? Get(collection, "Length");
            var count = ToInt(countObject, 0);
            for (var index = 0; index < count; index++)
            {
                var item = Invoke(collection, "get_Item", index);
                if (item != null) result.Add(item);
            }
            return result;
        }

        public string Text(object value)
        {
            return value == null ? string.Empty : value.ToString();
        }

        public double Number(object value, double fallback = 0)
        {
            if (value == null) return fallback;
            try { return Convert.ToDouble(value); }
            catch { return fallback; }
        }

        public bool Boolean(object value, bool fallback = false)
        {
            if (value == null) return fallback;
            try { return Convert.ToBoolean(value); }
            catch { return fallback; }
        }

        public int ToInt(object value, int fallback = 0)
        {
            if (value == null) return fallback;
            try { return Convert.ToInt32(value); }
            catch { return fallback; }
        }

        private object GetValue(Type type, object instance, string memberName, BindingFlags scope)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | scope;
            var key = type.AssemblyQualifiedName + "|" + scope + "|" + memberName;
            MemberInfo member;
            if (!_members.TryGetValue(key, out member))
            {
                member = (MemberInfo)type.GetProperty(memberName, flags) ?? type.GetField(memberName, flags);
                _members[key] = member;
            }
            var property = member as PropertyInfo;
            if (property != null) return property.GetValue(instance, null);
            var field = member as FieldInfo;
            return field == null ? null : field.GetValue(instance);
        }

        private static object[] AdaptArguments(ParameterInfo[] parameters, object[] arguments)
        {
            var result = new object[arguments.Length];
            for (var index = 0; index < arguments.Length; index++)
            {
                var argument = arguments[index];
                var targetType = parameters[index].ParameterType;
                if (argument is string && targetType.FullName == "Il2CppSystem.String")
                {
                    var conversion = targetType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .FirstOrDefault(method => method.Name == "op_Implicit" &&
                                                  method.GetParameters().Length == 1 &&
                                                  method.GetParameters()[0].ParameterType == typeof(string));
                    result[index] = conversion == null ? argument : conversion.Invoke(null, new[] { argument });
                }
                else
                {
                    result[index] = argument;
                }
            }
            return result;
        }
    }
}
