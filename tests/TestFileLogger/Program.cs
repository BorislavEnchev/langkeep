using Microsoft.Extensions.Logging;

var logsDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "LangKeep",
    "logs");

Console.WriteLine($"Logs directory: {logsDir}");
Console.WriteLine($"Directory exists: {Directory.Exists(logsDir)}");

// Manually replicate FileLogger logic
Directory.CreateDirectory(logsDir);
var now = DateTime.Now;
var datePart = now.ToString("yyyy-MM-dd");
var logFilePath = Path.Combine(logsDir, $"langkeep-test-{datePart}.log");
Console.WriteLine($"Log file path: {logFilePath}");

try
{
    File.AppendAllText(logFilePath, $"[{now:HH:mm:ss.fff}] [Test] This is a test log entry.{Environment.NewLine}");
    Console.WriteLine("Write succeeded!");
    var content = File.ReadAllText(logFilePath);
    Console.WriteLine($"Content: {content}");
}
catch (Exception ex)
{
    Console.WriteLine($"Write FAILED: {ex.GetType().Name}: {ex.Message}");
}
