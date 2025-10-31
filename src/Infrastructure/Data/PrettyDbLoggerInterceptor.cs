using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace pos_system_api.Infrastructure.Data;

/// <summary>
/// EF Core command interceptor that logs SQL commands (request) and execution results (response)
/// in a readable, pretty style using the application's ILogger (Serilog is configured in Program.cs).
/// </summary>
public class PrettyDbLoggerInterceptor : DbCommandInterceptor
{
    private readonly ILogger<PrettyDbLoggerInterceptor> _logger;
    private readonly ConcurrentDictionary<int, Stopwatch> _timers = new();

    public PrettyDbLoggerInterceptor(ILogger<PrettyDbLoggerInterceptor> logger)
    {
        _logger = logger;
    }

    private void LogRequest(DbCommand command)
    {
        try
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine("──────────────────────────────────────────────────────────");
            builder.AppendLine("📤 DB REQUEST");
            builder.AppendLine("──────────────────────────────────────────────────────────");
            builder.AppendLine("SQL:");
            builder.AppendLine(command.CommandText);
            if (command.Parameters.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("Parameters:");
                foreach (DbParameter p in command.Parameters)
                {
                    var value = p.Value == DBNull.Value ? "NULL" : p.Value;
                    builder.AppendLine($" - {p.ParameterName} = {value} ({p.DbType})");
                }
            }
            builder.AppendLine("──────────────────────────────────────────────────────────");
            _logger.LogInformation(builder.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log DB request");
        }
    }

    private void LogResponse(DbCommand command, long elapsedMs, object? result, CommandExecutedEventData eventData)
    {
        try
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine("──────────────────────────────────────────────────────────");
            builder.AppendLine("📥 DB RESPONSE");
            builder.AppendLine("──────────────────────────────────────────────────────────");
            builder.AppendLine($"Elapsed: {elapsedMs} ms");

            if (result is int rows)
            {
                builder.AppendLine($"Rows affected: {rows}");
            }
            else if (result is null)
            {
                builder.AppendLine("Result: (null)");
            }
            else
            {
                // For readers/scalars, we don't attempt to materialize results here
                builder.AppendLine($"Result type: {result.GetType().Name}");
            }

            if (eventData?.Command?.Connection?.Database != null)
            {
                builder.AppendLine($"Database: {eventData.Command.Connection.Database}");
            }

            builder.AppendLine("──────────────────────────────────────────────────────────");
            _logger.LogInformation(builder.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log DB response");
        }
    }

    private void StartTimer(DbCommand command)
    {
        var key = command.GetHashCode();
        var sw = new Stopwatch();
        _timers[key] = sw;
        sw.Start();
    }

    private long StopTimer(DbCommand command)
    {
        var key = command.GetHashCode();
        if (_timers.TryRemove(key, out var sw))
        {
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }
        return -1;
    }

    // NonQuery
    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        LogRequest(command);
        StartTimer(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        var elapsed = StopTimer(command);
        LogResponse(command, elapsed, result, eventData);
        return base.NonQueryExecuted(command, eventData, result);
    }

    // Scalar
    public override InterceptionResult<object?> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object?> result)
    {
        LogRequest(command);
        StartTimer(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        var elapsed = StopTimer(command);
        LogResponse(command, elapsed, result, eventData);
        return base.ScalarExecuted(command, eventData, result);
    }

    // Reader
    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        LogRequest(command);
        StartTimer(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        var elapsed = StopTimer(command);
        LogResponse(command, elapsed, result, eventData);
        return base.ReaderExecuted(command, eventData, result);
    }

    // Async variants
    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        LogRequest(command);
        StartTimer(command);
        return await base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override async ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        var elapsed = StopTimer(command);
        LogResponse(command, elapsed, result, eventData);
        return await base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override async ValueTask<InterceptionResult<object?>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object?> result, CancellationToken cancellationToken = default)
    {
        LogRequest(command);
        StartTimer(command);
        return await base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override async ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    {
        var elapsed = StopTimer(command);
        LogResponse(command, elapsed, result, eventData);
        return await base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        LogRequest(command);
        StartTimer(command);
        return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        var elapsed = StopTimer(command);
        LogResponse(command, elapsed, result, eventData);
        return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }
}
