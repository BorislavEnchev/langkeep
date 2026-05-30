using Microsoft.Extensions.Logging;

namespace LangKeep.Infrastructure.Windows.Logging;

/// <summary>
/// A lightweight file logger that writes log entries to daily rolling files
/// under <c>%AppData%\LangKeep\logs\</c>.
/// </summary>
internal sealed class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly string _logsDirectory;
    private readonly object _lock = new();

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    /// <summary>
    /// Initializes a new instance of the <see cref="FileLogger"/> class.
    /// </summary>
    public FileLogger(string categoryName, string logsDirectory)
    {
        _categoryName = categoryName;
        _logsDirectory = logsDirectory;
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Trace;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var now = DateTime.Now;
        var datePart = now.ToString("yyyy-MM-dd");
        var timePart = now.ToString("HH:mm:ss.fff");
        var message = formatter(state, exception);

        var logEntry = $"[{timePart}] [{logLevel,-11}] [{_categoryName}] {message}";
        if (exception is not null)
            logEntry += $"{Environment.NewLine}{exception}";

        var logFilePath = Path.Combine(_logsDirectory, $"langkeep-{datePart}.log");

        lock (_lock)
        {
            try
            {
                Directory.CreateDirectory(_logsDirectory);

                if (File.Exists(logFilePath) && new FileInfo(logFilePath).Length > MaxFileSizeBytes)
                {
                    logFilePath = Path.Combine(_logsDirectory, $"langkeep-{datePart}-{Guid.NewGuid():N}.log");
                }

                File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
            }
            catch
            {
                // Best-effort — never crash the app due to logging.
            }
        }
    }
}
