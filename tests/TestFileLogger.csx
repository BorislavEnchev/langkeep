#r "C:\Users\kiborko\Desktop\GitHub\langkeep\src\LangKeep.Infrastructure.Windows\bin\Release\net9.0-windows\LangKeep.Infrastructure.Windows.dll"

using LangKeep.Infrastructure.Windows.Logging;
using Microsoft.Extensions.Logging;

var logsDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "LangKeep",
    "logs");

Console.WriteLine($"Logs directory: {logsDir}");
Console.WriteLine($"Directory exists: {Directory.Exists(logsDir)}");

// Create provider directly
var provider = new FileLoggerProvider(logsDir);
var logger = provider.CreateLogger("TestLogger");

logger.LogInformation("Test INFORMATION message from test script");
logger.LogDebug("Test DEBUG message from test script");
logger.LogTrace("Test TRACE message from test script");

Console.WriteLine("Log messages sent. Checking if files were created...");

var files = Directory.GetFiles(logsDir, "*.log");
if (files.Length > 0)
{
    Console.WriteLine($"Found {files.Length} log file(s):");
    foreach (var f in files)
    {
        var content = File.ReadAllText(f);
        Console.WriteLine($"  {f}: {content.Trim()}");
    }
}
else
{
    Console.WriteLine("NO LOG FILES CREATED!");
    Console.WriteLine("Checking directory contents:");
    foreach (var f in Directory.GetFiles(logsDir))
    {
        Console.WriteLine($"  {f}");
    }
}
