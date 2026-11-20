namespace NyxMonitor.Core.Abstractions;

public interface IAppLogger
{
    void Info(string message);
    void Warning(string message, Exception? exception = null);
    void Error(string message, Exception? exception = null);
}
