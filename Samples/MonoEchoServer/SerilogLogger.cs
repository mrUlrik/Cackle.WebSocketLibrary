using Serilog.Events;
using Serilog;
using vtortola.WebSockets;

namespace EchoServer;

/// <summary>
///     Provides logging to <see cref="vtortola.WebSockets.ILogger" />.
/// </summary>
internal sealed class SerilogLogger : vtortola.WebSockets.ILogger
{
    private readonly Serilog.ILogger log = Log.ForContext<WebSocketListener>();

    /// <summary>
    ///     Writes a log event with the <see cref="LogEventLevel.Debug" /> level and associated exception.
    /// </summary>
    /// <param name="message">Message template describing the event.</param>
    /// <param name="error">Exception related to the event.</param>
    public void Debug(string message, Exception error = null)
    {
        log.Debug(error, message);
    }

    /// <summary>
    ///     Writes a log event with the <see cref="LogEventLevel.Warning" /> level and associated exception.
    /// </summary>
    /// <param name="message">Message template describing the event.</param>
    /// <param name="error">Exception related to the event.</param>
    public void Warning(string message, Exception error = null)
    {
        log.Warning(error, message);
    }

    /// <summary>
    ///     Writes a log event with the <see cref="LogEventLevel.Error" /> level and associated exception.
    /// </summary>
    /// <param name="message">Message template describing the event.</param>
    /// <param name="error">Exception related to the event.</param>
    public void Error(string message, Exception error = null)
    {
        log.Error(error, message);
    }

    /// <summary>
    ///     Determine if events at the <see cref="LogEventLevel.Debug" /> level will be passed through to the log sinks.
    /// </summary>
    /// <returns><see langword="true" /> if the level is enabled; otherwise, <see langword="false" />.</returns>
    public bool IsDebugEnabled => Log.IsEnabled(LogEventLevel.Debug);

    /// <summary>
    ///     Determine if events at the <see cref="LogEventLevel.Warning" /> level will be passed through to the log sinks.
    /// </summary>
    /// <returns><see langword="true" /> if the level is enabled; otherwise, <see langword="false" />.</returns>
    public bool IsWarningEnabled => Log.IsEnabled(LogEventLevel.Warning);

    /// <summary>
    ///     Determine if events at the <see cref="LogEventLevel.Error" /> level will be passed through to the log sinks.
    /// </summary>
    /// <returns><see langword="true" /> if the level is enabled; otherwise, <see langword="false" />.</returns>
    public bool IsErrorEnabled => Log.IsEnabled(LogEventLevel.Error);
}