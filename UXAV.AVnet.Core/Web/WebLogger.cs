using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using UXAV.Logging;

namespace UXAV.AVnet.Core.Web;

public class WebLogger : ILogger
{
    private readonly string _name;

    public WebLogger(string name)
    {
        _name = name;
    }

    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message)) return;

        if (exception != null)
        {
            Logger.Error(_name + ": " + exception);
            return;
        }

        switch (logLevel)
        {
            case LogLevel.Debug:
            case LogLevel.Trace:
                Logger.Debug(_name + ": " + message);
                break;
            case LogLevel.Information:
                Logger.Log(_name + ": " + message);
                break;
            case LogLevel.Warning:
                Logger.Warn(_name + ": " + message);
                break;
            case LogLevel.Error:
            case LogLevel.Critical:
                Logger.Error(_name + ": " + message);
                break;
        }
    }
}

public class WebLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, WebLogger> _loggers = new ConcurrentDictionary<string, WebLogger>();

    public WebLoggerProvider()
    {
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new WebLogger(name));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}
