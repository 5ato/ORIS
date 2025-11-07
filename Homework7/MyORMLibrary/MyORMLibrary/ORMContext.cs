using Npgsql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace MyORMLibrary;

public class ORMContext : IORMContext
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly string _connectionString;

    public ORMContext(IConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public void Create<T>(T entity, string tableName) where T : class
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        var typeT = typeof(T);
        var properties = typeT.GetProperties();

        var columnNames = string.Join(", ", properties.Select(p => $"\"{p.Name}\""));
        var parameterNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));

        var command = connection.CreateCommand();
        command.CommandText = $"INSERT INTO {tableName} ({columnNames}) VALUES ({parameterNames}) ";

        foreach (var property in properties)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = $"@{property.Name}";
            parameter.Value = property.GetValue(entity) ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        command.ExecuteNonQuery();
    }

    public T? ReadById<T>(int id, string tableName) where T : class
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM {tableName} WHERE Id = @id";

        var param = command.CreateParameter();
        param.ParameterName = "@id";
        param.Value = id;
        command.Parameters.Add(param);

        using var reader = command.ExecuteReader();

        T? user = null;

        if (reader.Read())
        {
            user = Map<T>(reader);
        }
        return user;
    }

    public List<T> ReadByAll<T>(string tableName) where T : class
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM {tableName}";

        using var reader = command.ExecuteReader();

        List<T> result = [];

        while (reader.Read())
        {
            result.Add(Map<T>(reader));
        }
        return result;
    }

    public void Update<T>(int id, T entity, string tableName)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        var typeT = typeof(T);
        var properties = typeT.GetProperties();

        var columnNames = string.Join(", ", properties.Select(p => $"\"{p.Name}\" = @{p.Name}"));

        var command = connection.CreateCommand();
        command.CommandText = $"UPDATE {tableName} SET {columnNames} WHERE Id = @id";

        var param = command.CreateParameter();
        param.ParameterName = "@id";
        param.Value = id;
        command.Parameters.Add(param);

        foreach (var property in properties)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = $"@{property.Name}";
            parameter.Value = property.GetValue(entity) ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        command.ExecuteNonQuery();
    }

    public void Delete(int id, string tableName)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM {tableName} WHERE Id = @id"; ;

        var param = command.CreateParameter();
        param.ParameterName = "@id";
        param.Value = id;
        command.Parameters.Add(param);

        command.ExecuteNonQuery();
    }

    private string ParseExpression(Expression expression)
    {
        if (expression is BinaryExpression binary)
        {
            // разбираем выражение на составляющие
            var left = ParseExpression(binary.Left);  // Левая часть выражения
            var right = ParseExpression(binary.Right); // Правая часть выражения
            var op = GetSqlOperator(binary.NodeType);  // Оператор (например, > или =)
            return $"({left} {op} {right})";
        }
        else if (expression is MemberExpression member)
        {
            return member.Member.Name; // Название свойства
        }
        else if (expression is ConstantExpression constant)
        {
            return FormatConstant(constant.Value); // Значение константы
        }
        else if (expression is MethodCallExpression method)
        {
            return GetConditionFromMethodCall(method);
        }

        // TODO: можно расширить для поддержки более сложных выражений (например, методов Contains, StartsWith и т.д.).
        // если не поддерживается то выбрасываем исключение
        throw new NotSupportedException($"Unsupported expression type: {expression.GetType().Name}");
    }

    private string GetConditionFromMethodCall(MethodCallExpression methodCall)
    {
        return methodCall.Method.Name switch
        {
            "Contains" => ProcessContains(methodCall),
            "StartsWith" => ProcessStartsWith(methodCall),
            "EndsWith" => ProcessEndsWith(methodCall),
            _ => throw new NotSupportedException($"Unsupported method: {methodCall.Method.Name}")
        };
    }

    private string ProcessContains(MethodCallExpression methodCall)
    {
        if (methodCall.Object is not MemberExpression member)
            throw new NotSupportedException($"Unsupported expression type: {methodCall.Object?.GetType().Name}");

        if (member.Expression is not ConstantExpression container)
            throw new NotSupportedException($"Unsupported container type: {member.Expression?.GetType().Name}");

        if (methodCall.Arguments.Count != 1)
            throw new NotSupportedException($"Contains method must have exactly 1 argument");

        var argumentMember = methodCall.Arguments[0];
        if (argumentMember is not MemberExpression argMember)
            throw new NotSupportedException($"Unsupported argument type: {argumentMember.GetType().Name}");

        var fieldInfo = member.Member as FieldInfo;
        var propertyInfo = member.Member as PropertyInfo;

        object? data = fieldInfo != null
            ? fieldInfo.GetValue(container.Value)
            : propertyInfo?.GetValue(container.Value);

        if (data == null)
            throw new NotSupportedException($"Could not extract value from member: {member.Member.Name}");

        if (data is not IEnumerable enumerable)
            throw new NotSupportedException($"Member value must be enumerable: {member.Member.Name}");

        return $"{argMember.Member.Name} IN ({string.Join(", ", enumerable.Cast<object>())})";
    }

    private string ProcessStartsWith(MethodCallExpression methodCall)
    {
        if (methodCall.Object is not MemberExpression member)
            throw new NotSupportedException($"Unsupported expression type: {methodCall.Object?.GetType().Name}");

        if (methodCall.Arguments.Count != 1)
            throw new NotSupportedException($"StartsWith method must have exactly 1 argument");

        var argumentMember = methodCall.Arguments[0];
        if (argumentMember is not ConstantExpression constant)
            throw new NotSupportedException($"Unsupported argument type: {argumentMember.GetType().Name}");

        return $"{member.Member.Name} LIKE N'{constant.Value}%'";
    }

    private string ProcessEndsWith(MethodCallExpression methodCall)
    {
        if (methodCall.Object is not MemberExpression member)
            throw new NotSupportedException($"Unsupported expression type: {methodCall.Object?.GetType().Name}");

        if (methodCall.Arguments.Count != 1)
            throw new NotSupportedException($"EndsWith method must have exactly 1 argument");

        var argumentMember = methodCall.Arguments[0];
        if (argumentMember is not ConstantExpression constant)
            throw new NotSupportedException($"Unsupported argument type: {argumentMember.GetType().Name}");

        return $"{member.Member.Name} LIKE N'%{constant.Value}'";
    }

    private string GetSqlOperator(ExpressionType nodeType)
    {
        return nodeType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.AndAlso => "AND",
            ExpressionType.OrElse => "OR",
            ExpressionType.NotEqual => "<>",
            ExpressionType.GreaterThan => ">",
            ExpressionType.LessThan => "<",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThanOrEqual => "<=",
            _ => throw new NotSupportedException($"Unsupported node type: {nodeType}")
        };
    }

    private string FormatConstant(object value)
    {
        return value is string ? $"'{value}'" : value.ToString();
    }

    private string BuildSqlQuery<T>(Expression<Func<T, bool>> predicate, bool singleResult)
    {
        var tableName = typeof(T).Name + "s"; // Имя таблицы, основанное на имени класса
        var whereClause = ParseExpression(predicate.Body);
        var limitClause = singleResult ? "LIMIT 1" : string.Empty;

        return $"SELECT * FROM {tableName} WHERE {whereClause} {limitClause}".Trim();
    }

    public T? FirstOrDefault<T>(Expression<Func<T, bool>> predicate)
    {
        var sqlQuery = BuildSqlQuery(predicate, singleResult: true);
        return ExecuteQuerySingle<T>(sqlQuery);
    }

    public IEnumerable<T> Where<T>(Expression<Func<T, bool>> predicate)
    {
        var sqlQuery = BuildSqlQuery(predicate, singleResult: false);
        return ExecuteQueryMultiple<T>(sqlQuery);
    }

    private T? ExecuteQuerySingle<T>(string query)
    {
        using var connection = _connectionFactory.CreateConnection();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = query;
            connection.Open();
            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    return Map<T>(reader);
                }
            }
        }
        return default;
    }

    private IEnumerable<T> ExecuteQueryMultiple<T>(string query)
    {
        var results = new List<T>();

        using var connection = _connectionFactory.CreateConnection();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = query;
            connection.Open();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    results.Add(Map<T>(reader));
                }
            }
        }
        return results;
    }

    private T Map<T>(IDataReader reader)
    {
        var typeT = typeof(T);
        var properties = typeT.GetProperties()
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        var newExp = Expression.New(typeT);
        List<MemberBinding> bindings = [];
        for (int i = 0; i < reader.FieldCount; i++)
        {
            var nameColumn = reader.GetName(i);
            var valueColumn = reader.GetValue(i);

            if (properties.TryGetValue(nameColumn, out var value))
                bindings.Add(Expression.Bind(value, Expression.Constant(valueColumn)));
        }

        var memberInit = Expression.MemberInit(newExp, bindings);
        return Expression.Lambda<Func<T>>(memberInit).Compile()();
    }
}

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class UsersWithoudId
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}