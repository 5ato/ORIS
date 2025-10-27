using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

namespace MiniTemplateEngine;

public class HtmlTemplateRenderer : IHtmlTemplateRenderer
{
    public string RenderFromString(string htmlTemplate, object dataModel)
    {
        var data = ToDictionary(dataModel);
        return Render(htmlTemplate, data);
    }
    public string Render(string htmlTemplate, Dictionary<string, object?> dataModel)
    {
        var result = new StringBuilder();
        int lastSeen = 0;

        for (int i = 0; i < htmlTemplate.Length; i++)
        {
            if (htmlTemplate[i] == '$')
            {
                string keyWord = GetKeyWord(htmlTemplate, i, ref lastSeen);

                if (keyWord.Equals("if", StringComparison.OrdinalIgnoreCase))
                {
                    string condition = GetCondition(htmlTemplate, lastSeen, ref lastSeen);

                    var resultCondition = EvaluateCondition(dataModel, condition);
                    var (ifBody, elseBody) = GetIfBody(htmlTemplate, ref lastSeen);

                    var renderBody = resultCondition ? ifBody : elseBody;
                    result.Append(Render(renderBody, dataModel));

                    i = lastSeen;
                    continue;
                }
                else if (keyWord.Equals("foreach", StringComparison.OrdinalIgnoreCase))
                {
                    string condition = GetCondition(htmlTemplate, lastSeen, ref lastSeen);

                    var conditionWords = condition.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (conditionWords.Length != 4 ||
                        !conditionWords[0].Equals("var", StringComparison.OrdinalIgnoreCase) ||
                        !conditionWords[2].Equals("in", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Неправильное использование");

                    string variableName = conditionWords[1];
                    var collections = GetValueByPath(dataModel, conditionWords[3]) as
                            IEnumerable ??
                            throw new InvalidOperationException("Забыли передать данные");

                    string body = GetForeachBody(htmlTemplate, ref lastSeen);

                    foreach (var item in collections)
                    {
                        var type = item.GetType();
                        var value = type.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance).GetValue(item);
                        var localModel = new Dictionary<string, object?>(dataModel, StringComparer.OrdinalIgnoreCase)
                        {
                            [variableName] = value
                        };

                        result.Append(Render(body, localModel));
                    }
                    i = lastSeen;
                    continue;
                }
            }

            if (htmlTemplate[i] == '$' && i + 1 < htmlTemplate.Length && htmlTemplate[i + 1] == '{')
            {
                int end = htmlTemplate.IndexOf('}', i + 2);
                if (end == -1)
                    throw new InvalidOperationException("Нету закрывающей скобки");

                string valueString = htmlTemplate[(i + 2)..end];
                var value = GetValueByPath(dataModel, valueString)
                    ?? throw new InvalidOperationException("Отсутствует значение в модели");

                result.Append(value.ToString());
                i = end;
            }
            else
                result.Append(htmlTemplate[i]);
        }
        return result.ToString();
    }

    private string GetForeachBody(string htmlTemplate, ref int startIndex)
    {
        var foreachBody = new StringBuilder();

        int depth = 0;

        for (int i = startIndex; i < htmlTemplate.Length; i++)
        {
            if (htmlTemplate[i] == '$')
            {
                string keyWord = GetKeyWord(htmlTemplate, i, ref startIndex);
                if (keyWord.Equals("foreach", StringComparison.OrdinalIgnoreCase))
                    depth++;
                else if (keyWord.Equals("endfor", StringComparison.OrdinalIgnoreCase))
                {
                    if (depth == 0)
                        return foreachBody.ToString();
                    depth--;
                }
            }
            foreachBody.Append(htmlTemplate[i]);
            startIndex = i;
        }
        throw new InvalidOperationException("Не найден $endfor");
    }

    private (string ifBody, string elseBody) GetIfBody(string htmlTemplate, ref int startIndex)
    {
        var ifBody = new StringBuilder();
        var elseBody = new StringBuilder();

        bool inElse = false;

        int depth = 0;

        for (int i = startIndex; i < htmlTemplate.Length; i++)
        {
            if (htmlTemplate[i] == '$')
            {
                string keyWord = GetKeyWord(htmlTemplate, i, ref startIndex);
                if (keyWord.Equals("if", StringComparison.OrdinalIgnoreCase))
                    depth++;
                else if (keyWord.Equals("endif", StringComparison.OrdinalIgnoreCase))
                {
                    if (depth == 0)
                        return (ifBody.ToString(), elseBody.ToString());
                    depth--;
                }
                else if (keyWord.Equals("else", StringComparison.OrdinalIgnoreCase) && depth == 0)
                {
                    inElse = true;
                    i = startIndex;
                    continue;
                }
            }

            if (inElse)
                elseBody.Append(htmlTemplate[i]);
            else
                ifBody.Append(htmlTemplate[i]);
            startIndex = i;
        }

        throw new InvalidOperationException("Не найден $endif");
    }

    private string GetCondition(string htmlTemplate, int startIndex, ref int lastSeen)
    {
        StringBuilder condition = new();

        for (int j = htmlTemplate.IndexOf('(', startIndex) + 1; htmlTemplate[j] != ')'; j++)
        {
            condition.Append(htmlTemplate[j]);
            lastSeen = j + 2;
        }

        return condition.ToString();
    }

    private Dictionary<string, object?> ToDictionary(object obj)
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

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime);
    }

    private string GetKeyWord(string htmlTemplate, int startIndex, ref int lastSeen)
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

    private bool EvaluateCondition(Dictionary<string, object?> obj, string condition)
    {
        var gtMatch = Regex.Match(condition, @"([^>]+)\s*>\s*([^>]+)");
        if (gtMatch.Success)
        {
            var left = GetValueByPath(obj, gtMatch.Groups[1].Value.Trim());
            var right = GetValueByPath(obj, gtMatch.Groups[2].Value.Trim());

            if (left is IComparable leftComp && right is IComparable rightComp)
            {
                return leftComp.CompareTo(rightComp) > 0;
            }
        }

        // Булевы значения
        var value = GetValueByPath(obj, condition);
        return value is bool boolValue ? boolValue : false;
    }

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
            } else
            {
                var prop = current?.GetType().GetProperty(part, BindingFlags.Instance | BindingFlags.Public);
                current = prop?.GetValue(current);
            }
        }

        return current;
    }

    public string RenderFromFile(string filePath, object dataModel)
    {
        throw new NotImplementedException();
    }

    public string RenderToFile(string inputFilePath, string outputFilePath, object dataModel)
    {
        throw new NotImplementedException();
    }

}
