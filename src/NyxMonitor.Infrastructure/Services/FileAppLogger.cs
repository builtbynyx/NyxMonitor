using System.Text;
using NyxMonitor.Core.Abstractions;

namespace NyxMonitor.Infrastructure.Services;

/// <summary>Minimal thread-safe file logger. Logging never throws.</summary>
public sealed class FileAppLogger : IAppLogger
{
    private const long MaxBytes = 1024 * 1024;
    private readonly object _gate = new();
    private readonly string _path;

    public FileAppLogger()
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BuiltByNyx", "NyxMonitor", "logs");
        _path = Path.Combine(directory, "nyxmonitor.log");

        try
        {
            Directory.CreateDirectory(directory);
            var info = new FileInfo(_path);
            if (info.Exists && info.Length > MaxBytes)
            {
                File.Move(_path, _path + ".old", overwrite: true);
            }
        }
        catch (Exception)
        {
            // Logging must never prevent the app from starting.
        }
    }

    public string LogPath => _path;

    public void Info(string message) => Write("INFO", message, null);
    public void Warning(string message, Exception? exception = null) => Write("WARN", message, exception);
    public void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    private void Write(string level, string message, Exception? exception)
    {
        var line = new StringBuilder()
            .Append(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
            .Append(' ').Append(level).Append(' ').Append(message);
        if (exception is not null)
        {
            line.AppendLine().Append(exception);
        }

        line.AppendLine();

        lock (_gate)
        {
            try
            {
                File.AppendAllText(_path, line.ToString());
            }
            catch (Exception)
            {
                // Ignore I/O failures while logging.
            }
        }
    }
}
