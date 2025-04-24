using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System.Globalization;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

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
    public static void TrySetField(this JSONObject jsonObj, string fieldName, bool data)
    {
        if (!jsonObj.IsNull && jsonObj.HasField(fieldName))
            jsonObj.SetField(fieldName, data);
        else
            jsonObj.AddField(fieldName, data);
    }
    public static void TrySetField(this JSONObject jsonObj, string fieldName, float data)
    {
        if (!jsonObj.IsNull && jsonObj.HasField(fieldName))
            jsonObj.SetField(fieldName, data);
        else
            jsonObj.AddField(fieldName, data);
    }
    public static void TrySetField(this JSONObject jsonObj, string fieldName, int data)
    {
        if (!jsonObj.IsNull && jsonObj.HasField(fieldName))
            jsonObj.SetField(fieldName, data);
        else
            jsonObj.AddField(fieldName, data);
    }
    public static void TrySetField(this JSONObject jsonObj, string fieldName, JSONObject data)
    {
        if (!jsonObj.IsNull && jsonObj.HasField(fieldName))
            jsonObj.SetField(fieldName, data);
        else
            jsonObj.AddField(fieldName, data);
    }
    public static void TrySetField(this JSONObject jsonObj, string fieldName, string data)
    {
        if (!jsonObj.IsNull && jsonObj.HasField(fieldName))
            jsonObj.SetField(fieldName, JSONObject.CreateStringObject(data));
        else
            jsonObj.AddField(fieldName, JSONObject.CreateStringObject(data));
    }

    public static bool NotWorldSave(this ModSetting.MenuType type) => type != ModSetting.MenuType.World && type != ModSetting.MenuType.WorldCustom;

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
}
