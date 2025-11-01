using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

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

        var typeT = typeof(T);
        var properties = typeT.GetProperties()
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
        T? user = null;

        if (reader.Read())
        {
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
            user = Expression.Lambda<Func<T>>(memberInit).Compile()();
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
        var typeT = typeof(T);
        var properties = typeT.GetProperties()
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        while (reader.Read())
        {
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
            var user = Expression.Lambda<Func<T>>(memberInit).Compile()();

            result.Add(user);
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