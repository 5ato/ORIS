using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MiniTemplateEngine;

public static class Utils
{
    public static object? GetValueByPath(Dictionary<string, object?> obj, string path)
    {
        if (obj == null || string.IsNullOrEmpty(path))
            return null;

        var parts = path.Split('.');
        object? current = obj;

        foreach (var part in parts)
        {
            if (current is Dictionary<string, object?> dict)
            {
                if (!dict.TryGetValue(part, out current))
                    return null;
            }
            else
            {
                var prop = current?.GetType().GetProperty(part, BindingFlags.Instance | BindingFlags.Public);
                current = prop?.GetValue(current);
            }
        }

        return current;
    }

    public static Dictionary<string, object?> ToDictionary(object obj)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (obj == null) return result;

        if (obj is Dictionary<string, object?> dict)
            return new Dictionary<string, object?>(dict, StringComparer.OrdinalIgnoreCase);

        if (obj is IEnumerable enumerable && obj is not string)
        {
            int index = 0;
            foreach (var item in enumerable)
            {
                result[index++.ToString()] = ToDictionary(item);
            }
            return result;
        }

        var type = obj.GetType();

        if (IsSimpleType(type))
            return new Dictionary<string, object?>() { ["value"] = obj };

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var val = prop.GetValue(obj);
            if (val != null && !IsSimpleType(val.GetType()))
                result[prop.Name] = ToDictionary(val);
            else
                result[prop.Name] = val;
        }
        return result;
    }

    public static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime);
    }

    public static string ExtractKeyWord(string htmlTemplate, int startIndex, ref int lastSeen)
    {
        var keyWordBuilder = new StringBuilder();

        int i = startIndex + 1;

        while (i < htmlTemplate.Length && char.IsWhiteSpace(htmlTemplate[i]))
        {
            i++;
        }

        for (; i < htmlTemplate.Length && htmlTemplate[i] != ' ' && htmlTemplate[i] != '('; i++)
        {
            keyWordBuilder.Append(htmlTemplate[i]);
            lastSeen = i;
        }
        return keyWordBuilder.ToString();
    }

    public static object? GetItemValue(object item)
    {
        // Try to get Value property, otherwise use the item itself
        return item.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance)?.GetValue(item)
            ?? item;
    }
}
