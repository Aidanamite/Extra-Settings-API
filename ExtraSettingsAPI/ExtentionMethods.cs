using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System.Globalization;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;

namespace _ExtraSettingsAPI
{
    static public class ExtentionMethods
    {
        public static int IndexOf<T>(this T[] array, T item)
        {
            return Array.IndexOf(array, item);
        }
        public static GameObject AddImageObject(this GameObject gameObject, float thickness)
        {
            GameObject imageContainer = new GameObject("Divider");
            RectTransform trans = imageContainer.AddComponent<RectTransform>();
            imageContainer.transform.SetParent(gameObject.transform, false);
            Image image = imageContainer.AddComponent<Image>();
            trans.anchorMin = new Vector2(0, 0);
            trans.anchorMax = new Vector2(1, 0);
            trans.offsetMin = new Vector2(10, 0);
            trans.offsetMax = new Vector2(-10, thickness);
            image.sprite = ExtraSettingsAPI.dividerSprite;
            return imageContainer;
        }
        public static void SetField_(this JSONObject obj, string fieldName, JSONObject value, bool ignoreCase = false)
        {
            if (obj.list == null)
                obj.list = new List<JSONObject>();
            if (obj.keys == null)
            {
                obj.keys = new List<string>(obj.list.Capacity);
                for (int i = 0; i < obj.list.Count; i++)
                    obj.keys.Add(i.ToString());
            }
            obj.type = JSONObject.Type.OBJECT;
            var compare = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            for (int i = 0; i < obj.keys.Count; i++)
                if (string.Equals(obj.keys[i], fieldName, compare))
                {
                    obj.list[i] = value;
                    obj.keys[i] = fieldName;
                    return;
                }
            obj.list.Add(value);
            obj.keys.Add(fieldName);
            return;
        }
        public static bool TryGetField(this JSONObject obj, string fieldName, out JSONObject result, bool ignoreCase = false)
        {
            if (obj.type != JSONObject.Type.OBJECT)
            {
                result = null;
                return false;
            }
            var compare = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            for (int i = 0; i < obj.keys.Count; i++)
                if (string.Equals(obj.keys[i], fieldName, compare))
                {
                    result = obj.list[i];
                    return true;
                }
            result = null;
            return false;
        }
        public static JSONObject GetOrAddField(this JSONObject obj, string fieldName, bool ignoreCase = false)
        {
            if (!obj.TryGetField(fieldName, out var result, ignoreCase))
                obj.AddField(fieldName, result = new JSONObject());
            return result;
        }
        public static X GetOrAddValue<X>(this JObject obj, string key, JTokenType? type = null, bool ignoreCase = false) where X : JToken, new()
        {
            if (obj.TryGetValue(key, out X existing, type, ignoreCase))
                return existing;
            var result = new X();
            obj[key] = result;
            return result;
        }
        public static X GetValue<X>(this JObject obj, string key, JTokenType? type = null, bool ignoreCase = false) where X : JToken
            => obj.TryGetValue(key, out X result, type, ignoreCase) ? result : null;
        public static bool TryGetValue<X>(this JObject obj, string key, out X result, JTokenType? type = null, bool ignoreCase = false) where X : JToken
        {
            if (obj.TryGetValue(key, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal, out var token) && (type == null || token.Type == type) && token is X _result)
            {
                result = _result;
                return true;
            }
            result = null;
            return false;
        }


        public static ModSetting.SaveType GetSaveType(this ModSetting.MenuType type)
            => type == ModSetting.MenuType.World || type == ModSetting.MenuType.WorldCustom ? ModSetting.SaveType.World
            : type == ModSetting.MenuType.Split || type == ModSetting.MenuType.SplitCustom ? ModSetting.SaveType.Split
            : type == ModSetting.MenuType.SplitStrong || type == ModSetting.MenuType.SplitStrongCustom ? ModSetting.SaveType.SplitStrong
            : ModSetting.SaveType.Global;

        public static bool IsSplit(this ModSetting.SaveType type) => type == ModSetting.SaveType.Split || type == ModSetting.SaveType.SplitStrong;

        public static string[] ToStringArray(this JSONObject obj)
        {
            var a = new string[obj.Count];
            for (int i = 0; i < a.Length; i++)
                a[i] = obj[i].str;
            return a;
        }

        public static float StepUp(this int v) => 1f / v;
        public static float StepDown(this int v) => 1 - ((v - 1f) / v);

        public static void PrintAllFields(this object obj)
        {
            var s = new StringBuilder(obj.ToString());
            var t = obj.GetType();
            while (t != typeof(object))
            {
                foreach (var f in t.GetFields(~BindingFlags.Default))
                    if (!f.IsStatic)
                    {
                        s.Append("\n - ");
                        s.Append(f.FieldType.FullName);
                        s.Append(" ");
                        s.Append(f.DeclaringType.FullName);
                        s.Append(".");
                        s.Append(f.Name);
                        s.Append(" = ");
                        s.Append(f.GetValue(obj));
                    }
                foreach (var p in t.GetProperties(~BindingFlags.Default))
                    if (p.GetGetMethod() != null && !p.GetGetMethod().IsStatic && p.GetGetMethod().GetParameters().Length == 0)
                    {
                        s.Append("\n - ");
                        s.Append(p.GetGetMethod().ReturnType.FullName);
                        s.Append(" ");
                        s.Append(p.DeclaringType.FullName);
                        s.Append(".");
                        s.Append(p.Name);
                        s.Append(" = ");
                        s.Append(p.GetValue(obj));
                    }
                t = t.BaseType;
            }
            Debug.Log(s.ToString());
        }

        public static string CamelToWords(this string original)
        {
            if (original == null)
                return null;
            var s = new StringBuilder();
            for (int i = 0; i < original.Length; i++)
            {
                if (i > 0 && char.ToLowerInvariant(original[i]) != char.ToUpperInvariant(original[i]) && original[i] == char.ToUpperInvariant(original[i]))
                    s.Append(' ');
                s.Append(original[i]);
            }
            return s.ToString();
        }

        public static T GetOrAddComponent<T>(this GameObject obj) where T : Component => obj.GetComponent<T>() ?? obj.AddComponent<T>();
        public static T GetOrAddComponent<T>(this Component obj) where T : Component => obj.GetComponent<T>() ?? obj.gameObject.AddComponent<T>();
        public static Object GetOrAddComponent(this GameObject obj, Type component) => obj.GetComponent(component) ?? obj.AddComponent(component);
        public static Object GetOrAddComponent(this Component obj, Type component) => obj.GetComponent(component) ?? obj.gameObject.AddComponent(component);

        public static Exception CleanInvoke(this Exception err) => err is TargetInvocationException && err.InnerException != null ? err.InnerException : err;
        public static bool TryConvert<T>(this object value, out T result)
        {
            if (value.TryConvert(typeof(T), out var result2))
            {
                result = (T)result2;
                return true;
            }
            result = default;
            return false;
        }
        public static bool TryConvert(this object value, Type targetType, out object result)
        {
            if (value.IsAssignableTo(targetType))
            {
                result = value;
                return true;
            }

            try
            {
                result = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
                return true;
            }
            catch { }

            if (targetType.IsEnum && value.TryConvert(targetType.GetEnumUnderlyingType(), out var result2))
            {
                result = result2;
                return true;
            }
            result = null;
            return false;
        }
        public static T EnsureType<T>(this object value)
        {
            value.TryConvert(out T result);
            return result;
        }

        public static bool IsNumber(this Type type, bool includeFloatingPointTypes = true)
            => type == typeof(sbyte)
            || type == typeof(byte)
            || type == typeof(short)
            || type == typeof(ushort)
            || type == typeof(int)
            || type == typeof(uint)
            || type == typeof(long)
            || type == typeof(ulong)
            || (includeFloatingPointTypes && (type == typeof(float) || type == typeof(double)));

        public static bool EnumTryParse(this Type type, string value, out object enumValue, bool ignoreCase = false)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (!type.IsEnum)
                throw new ArgumentException("Must an enum type", nameof(type));
            try
            {
                enumValue = Enum.Parse(type, value, ignoreCase);
                return true;
            }
            catch
            {
                enumValue = null;
                return false;
            }

        }

        public static T SafeGet<T>(this T[] array, int index, bool fallbackFirst = false, T fallback = default)
        {
            if (index < 0 || index >= array.Length)
                return fallbackFirst && array.Length > 0 ? array[0] : fallback;
            return array[index];
        }

        public static bool IsAssignableTo(this object obj, Type type)
        {
            if (obj == null)
            {
                if (type.IsValueType)
                    return false;
                return true;
            }
            else
                return type.IsAssignableFrom(obj.GetType());
        }

        public static bool CopyFrom<X, Y>(this IDictionary<X, Y> d, IDictionary<X, Y> other)
        {
            if (other == null || other.Count == 0)
            {
                if (d.Count == 0)
                    return false;
                d.Clear();
                return true;
            }
            var keysToCheck = new HashSet<X>(d.Keys);
            var changed = false;
            foreach (var p in other)
                if (!d.TryGetValue(p.Key, out var old) || Equals(old, p.Value))
                {
                    d[p.Key] = p.Value;
                    changed = true;
                    keysToCheck.Remove(p.Key);
                }
            if (keysToCheck.Count > 0)
            {
                changed = true;
                foreach (var k in keysToCheck)
                    d.Remove(k);
            }
            return changed;
        }

        public static bool SequenceEquals<X, Y>(this IEnumerable<X> first, IEnumerable<Y> second, Func<X, Y, bool> equals)
        {
            if (first == second)
                return true;
            if (first == null || second == null)
                return false;
            var a = first.GetEnumerator();
            var b = second.GetEnumerator();
            var c = a.MoveNext();
            var d = b.MoveNext();
            while (c && d)
            {
                if (!equals(a.Current, b.Current))
                    return false;
                c = a.MoveNext();
                d = b.MoveNext();
            }
            return c == d;
        }

        public static string Join<X>(this IEnumerable<X> items, Func<X, string> converter = null, string delimeter = ", ", Predicate<X> filter = null)
        {
            if (items == null)
                return string.Empty;
            var flag = false;
            var result = new StringBuilder();
            foreach (var item in items)
                if (filter == null || filter(item))
                {
                    if (flag)
                        result.Append(delimeter);
                    else
                        flag = true;
                    if (converter != null)
                        result.Append(converter(item));
                    else if (item != null)
                        result.Append(item.ToString());
                }
            return result.ToString();
        }

        public static Vector2 GetPreferredSize(this Text text, float maxWidth)
        {
            var size = new Vector2(Math.Min(text.preferredWidth, maxWidth), 0);
            size.y = text.cachedTextGeneratorForLayout.GetPreferredHeight(text.text, text.GetGenerationSettings(size)) / text.pixelsPerUnit;
            return size;
        }

        public static double Round(this double value, Rounding rounding = Rounding.Nearest)
            => rounding == Rounding.Lowest
            ? Math.Floor(value)
            : rounding == Rounding.Highest
            ? Math.Ceiling(value)
            : Math.Round(value, MidpointRounding.AwayFromZero);
    }
}