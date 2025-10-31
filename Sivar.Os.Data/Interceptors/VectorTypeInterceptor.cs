using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace Sivar.Os.Data.Interceptors;

/// <summary>
/// Interceptor to handle vector type conversions for pgvector
/// Automatically casts string parameters to vector type in INSERT/UPDATE commands
/// </summary>
public class VectorTypeInterceptor : DbCommandInterceptor
{
    private static readonly Regex VectorParameterRegex = new Regex(
        @"""ContentEmbedding""\s*[,)]", 
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
            // Find parameters that correspond to ContentEmbedding
            foreach (DbParameter param in command.Parameters)
            {
                if (param is NpgsqlParameter npgsqlParam)
                {
                    // Check if this parameter is for ContentEmbedding by examining the SQL
                    // We need to find the parameter position in the command text
                    var paramName = param.ParameterName;
                    
                    // If we find a string parameter that looks like it should be a vector
                    // we'll change its NpgsqlDbType to Unknown and let PostgreSQL handle the cast
                    if (param.Value is string stringValue && 
                        !string.IsNullOrEmpty(stringValue) &&
                        stringValue.StartsWith("[") && stringValue.EndsWith("]"))
                    {
                        // This looks like our vector format - cast it
                        npgsqlParam.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Unknown;
                        param.Value = $"{stringValue}::vector";
                    }
                }
            }
        }
    }
}
