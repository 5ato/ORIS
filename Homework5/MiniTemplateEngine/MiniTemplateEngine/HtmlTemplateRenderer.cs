using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

namespace MiniTemplateEngine;


public abstract class StrategyKeyWord
{
    public abstract void Algorithm(string htmlTemplate, Dictionary<string, object?> dataModel, StringBuilder result, ref int lastSeen, ref int position);
    protected static string ExtractCondition(string htmlTemplate, int startIndex, ref int lastSeen)
    {
        StringBuilder condition = new();

        for (int j = htmlTemplate.IndexOf('(', startIndex) + 1; htmlTemplate[j] != ')'; j++)
        {
            condition.Append(htmlTemplate[j]);
            lastSeen = j + 2;
        }

        return condition.ToString();
    }
}

public class ForeachStrategyKeyWord : StrategyKeyWord
{
    public override void Algorithm(string htmlTemplate, Dictionary<string, object?> dataModel, StringBuilder result, ref int lastSeen, ref int position)
    {
        string condition = ExtractCondition(htmlTemplate, lastSeen, ref lastSeen);

        var conditionWords = condition.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        ValidateConditionForeach(conditionWords);

        string variableName = conditionWords[1];
        var collections = Utils.GetValueByPath(dataModel, conditionWords[3]) as
                IEnumerable ??
                throw new InvalidOperationException("Забыли передать данные");

        string body = ExtractForeachBody(htmlTemplate, ref lastSeen);

        foreach (var item in collections)
        {
            var localModel = new Dictionary<string, object?>(dataModel, StringComparer.OrdinalIgnoreCase)
            {
                [variableName] = Utils.GetItemValue(item)
            };

            result.Append(HtmlTemplateRenderer.Render(body, localModel));
        }
        position = lastSeen;
    }

    private static void ValidateConditionForeach(string[] conditionWords)
    {
        if (conditionWords.Length != 4 ||
            !conditionWords[0].Equals("var", StringComparison.OrdinalIgnoreCase) ||
            !conditionWords[2].Equals("in", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Неправильное использование");
    }

    private static string ExtractForeachBody(string htmlTemplate, ref int startIndex)
    {
        var foreachBody = new StringBuilder();

        int depth = 0;

        for (int i = startIndex; i < htmlTemplate.Length; i++)
        {
            if (htmlTemplate[i] == '$')
            {
                string keyWord = Utils.ExtractKeyWord(htmlTemplate, i, ref startIndex);
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
}

public class IfStrategyKeyWord : StrategyKeyWord
{
    public override void Algorithm(string htmlTemplate, Dictionary<string, object?> dataModel, StringBuilder result, ref int lastSeen, ref int position)
    {
        string condition = ExtractCondition(htmlTemplate, lastSeen, ref lastSeen);

        var resultCondition = EvaluateCondition(dataModel, condition);
        var (ifBody, elseBody) = ExtractIfBody(htmlTemplate, ref lastSeen);

        var renderBody = resultCondition ? ifBody : elseBody;
        result.Append(HtmlTemplateRenderer.Render(renderBody, dataModel));

        position = lastSeen;
    }

    private static (string ifBody, string elseBody) ExtractIfBody(string htmlTemplate, ref int startIndex)
    {
        var ifBody = new StringBuilder();
        var elseBody = new StringBuilder();

        bool inElse = false;

        int depth = 0;

        for (int i = startIndex; i < htmlTemplate.Length; i++)
        {
            if (htmlTemplate[i] == '$')
            {
                string keyWord = Utils.ExtractKeyWord(htmlTemplate, i, ref startIndex);
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

    private static bool EvaluateCondition(Dictionary<string, object?> obj, string condition)
    {
        var gtMatch = Regex.Match(condition, @"([^>]+)\s*>\s*([^>]+)");
        if (gtMatch.Success)
        {
            var left = Utils.GetValueByPath(obj, gtMatch.Groups[1].Value.Trim());
            var right = Utils.GetValueByPath(obj, gtMatch.Groups[2].Value.Trim());

            if (left is IComparable leftComp && right is IComparable rightComp)
            {
                return leftComp.CompareTo(rightComp) > 0;
            }
        }

        // Булевы значения
        var value = Utils.GetValueByPath(obj, condition);
        return value is bool boolValue ? boolValue : false;
    }
}

public class VariableStrategyKeyWord : StrategyKeyWord
{
    public override void Algorithm(string htmlTemplate, Dictionary<string, object?> dataModel, StringBuilder result, ref int lastSeen, ref int position)
    {
        int end = htmlTemplate.IndexOf('}', position + 2);
        if (end == -1)
            throw new InvalidOperationException("Нету закрывающей скобки");

        string valueString = htmlTemplate[(position + 2)..end];
        var value = Utils.GetValueByPath(dataModel, valueString)
            ?? throw new InvalidOperationException("Отсутствует значение в модели");

        result.Append(value.ToString());
        position = end;
    }
}

public class TemplateProcessor
{
    public TemplateProcessor() { }
    public TemplateProcessor(StrategyKeyWord strategy) => Strategy = strategy;
    public StrategyKeyWord Strategy { get; set; }

    public void Execute(string htmlTemplate, Dictionary<string, object?> dataModel, StringBuilder result, ref int lastSeen, ref int position)
    {
        if (Strategy == null)
            throw new ArgumentException("Вы не передали стратегию");

        Strategy.Algorithm(htmlTemplate, dataModel, result, ref lastSeen, ref position);
    }
}


public class HtmlTemplateRenderer : IHtmlTemplateRenderer
{

    public string RenderFromString(string htmlTemplate, object dataModel)
    {
        var data = Utils.ToDictionary(dataModel);
        return Render(htmlTemplate, data);
    }

    public static string Render(string htmlTemplate, Dictionary<string, object?> dataModel)
    {
        var result = new StringBuilder();

        TemplateProcessor processor = new();

        for (int i = 0, lastSeen = 0; i < htmlTemplate.Length; i++)
        {
            if (htmlTemplate[i] == '$')
            {
                string keyWord = Utils.ExtractKeyWord(htmlTemplate, i, ref lastSeen);

                if (keyWord.Equals("if", StringComparison.OrdinalIgnoreCase))
                {
                    processor.Strategy = new IfStrategyKeyWord();
                    processor.Execute(htmlTemplate, dataModel, result, ref lastSeen, ref i);

                    continue;
                }
                else if (keyWord.Equals("foreach", StringComparison.OrdinalIgnoreCase))
                {
                    processor.Strategy = new ForeachStrategyKeyWord();
                    processor.Execute(htmlTemplate, dataModel, result, ref lastSeen, ref i);

                    continue;
                }
            }

            if (htmlTemplate[i] == '$' && i + 1 < htmlTemplate.Length && htmlTemplate[i + 1] == '{')
            {
                processor.Strategy = new VariableStrategyKeyWord();
                processor.Execute(htmlTemplate, dataModel, result, ref lastSeen, ref i);
            }
            else
                result.Append(htmlTemplate[i]);
        }
        return result.ToString();
    }

    public string RenderFromFile(string filePath, object dataModel)
    {
        return RenderFromString(File.ReadAllText(filePath, Encoding.UTF8), dataModel);
    }

    public string RenderToFile(string inputFilePath, string outputFilePath, object dataModel)
    {
        var result = RenderFromFile(inputFilePath, dataModel);
        File.WriteAllText(outputFilePath, result);
        return result;
    }

}
