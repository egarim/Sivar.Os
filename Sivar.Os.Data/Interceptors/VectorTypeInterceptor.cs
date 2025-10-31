using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace Sivar.Os.Data.Interceptors;

/// <summary>
/// Interceptor to handle vector type conversions for pgvector
/// Modifies SQL commands to cast string parameters to vector type
/// </summary>
public class VectorTypeInterceptor : DbCommandInterceptor
{
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

    private static void ModifyVectorCommand(DbCommand command)
    {
        // Only process INSERT and UPDATE commands that mention ContentEmbedding
        if ((command.CommandText.Contains("INSERT INTO \"Sivar_Posts\"") || 
             command.CommandText.Contains("UPDATE \"Sivar_Posts\"")) &&
            command.CommandText.Contains("ContentEmbedding"))
        {
            // Find the parameter that contains vector data (starts with "[" and ends with "]")
            string? vectorParamName = null;
            foreach (DbParameter param in command.Parameters)
            {
                if (param.Value is string stringValue && 
                    !string.IsNullOrEmpty(stringValue) &&
                    stringValue.StartsWith("[") && 
                    stringValue.EndsWith("]"))
                {
                    vectorParamName = param.ParameterName;
                    break;
                }
            }

            // If we found a vector parameter, modify the SQL to cast it
            if (vectorParamName != null)
            {
                // Replace @pX with @pX::vector in the SQL command
                // This tells PostgreSQL to cast the parameter to vector type
                command.CommandText = command.CommandText.Replace(
                    $"{vectorParamName}",
                    $"{vectorParamName}::vector");
            }
        }
    }
}
