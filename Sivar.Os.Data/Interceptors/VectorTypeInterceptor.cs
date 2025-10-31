using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data.Common;

namespace Sivar.Os.Data.Interceptors;

/// <summary>
/// Interceptor to handle vector type conversions for pgvector
/// Modifies SQL commands to cast string parameters to vector type
/// </summary>
public class VectorTypeInterceptor : DbCommandInterceptor
{
    private readonly ILogger<VectorTypeInterceptor>? _logger;

    public VectorTypeInterceptor(ILogger<VectorTypeInterceptor>? logger = null)
    {
        _logger = logger;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        ModifyVectorCommand(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        ModifyVectorCommand(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void ModifyVectorCommand(DbCommand command)
    {
        // Only process INSERT and UPDATE commands that mention ContentEmbedding
        if ((command.CommandText.Contains("INSERT INTO \"Sivar_Posts\"") || 
             command.CommandText.Contains("UPDATE \"Sivar_Posts\"")) &&
            command.CommandText.Contains("ContentEmbedding"))
        {
            _logger?.LogInformation("[VectorTypeInterceptor] Intercepted command with ContentEmbedding");
            _logger?.LogDebug("[VectorTypeInterceptor] Original SQL: {Sql}", command.CommandText);

            // Find the parameter that contains vector data (starts with "[" and ends with "]")
            string? vectorParamName = null;
            foreach (DbParameter param in command.Parameters)
            {
                _logger?.LogDebug("[VectorTypeInterceptor] Checking param {ParamName}, Value type: {ValueType}, Value: {Value}",
                    param.ParameterName,
                    param.Value?.GetType().Name ?? "null",
                    param.Value?.ToString()?.Substring(0, Math.Min(50, param.Value?.ToString()?.Length ?? 0)) ?? "null");

                if (param.Value is string stringValue && 
                    !string.IsNullOrEmpty(stringValue) &&
                    stringValue.StartsWith("[") && 
                    stringValue.EndsWith("]"))
                {
                    vectorParamName = param.ParameterName;
                    _logger?.LogInformation("[VectorTypeInterceptor] Found vector parameter: {ParamName} with value length {Length}",
                        vectorParamName, stringValue.Length);
                    break;
                }
            }

            // If we found a vector parameter, modify the SQL to cast it
            if (vectorParamName != null)
            {
                var originalSql = command.CommandText;
                // Replace @pX with @pX::vector in the SQL command
                // This tells PostgreSQL to cast the parameter to vector type
                command.CommandText = command.CommandText.Replace(
                    $"{vectorParamName}",
                    $"{vectorParamName}::vector");

                _logger?.LogInformation("[VectorTypeInterceptor] Modified SQL to add ::vector cast for {ParamName}",
                    vectorParamName);
                _logger?.LogDebug("[VectorTypeInterceptor] Modified SQL: {Sql}", command.CommandText);
            }
            else
            {
                _logger?.LogWarning("[VectorTypeInterceptor] No vector parameter found, but ContentEmbedding is in SQL");
            }
        }
    }
}
