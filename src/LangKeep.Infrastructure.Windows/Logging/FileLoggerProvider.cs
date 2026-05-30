using Microsoft.Extensions.Logging;

namespace LangKeep.Infrastructure.Windows.Logging;

/// <summary>
/// An <see cref="ILoggerProvider"/> that writes log entries to daily rolling files.
/// </summary>
internal sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logsDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileLoggerProvider"/> class.
    /// </summary>
    /// <param name="logsDirectory">The directory to write log files into.</param>
    public FileLoggerProvider(string logsDirectory)
    {
        _logsDirectory = logsDirectory;

        // Create the logs directory eagerly so it's ready by the time
        // the first log entry is written (prevents race conditions).
        try
        {
            Directory.CreateDirectory(_logsDirectory);
        }
        catch
        {
            // Best-effort — never crash due to logging setup.
        }
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, _logsDirectory);

    /// <inheritdoc />
    public void Dispose() { }
}
