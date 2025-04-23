using Serilog;
using Atune.Services;
using System;

namespace Atune.Services;

public class LoggerService : ILoggerService
{
    public void LogDebug(string message) => Log.Debug(message);
    public void LogError(string message, Exception? ex = null) => Log.Error(ex, message);
    public void LogInformation(string message) => Log.Information(message);
    public void LogWarning(string message) => Log.Warning(message);
} 
