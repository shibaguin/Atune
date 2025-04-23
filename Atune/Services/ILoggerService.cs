using System;

namespace Atune.Services;

public interface ILoggerService
{
    void LogInformation(string message);
    void LogError(string message, Exception? ex = null);
    void LogWarning(string message);
    void LogDebug(string message);
} 
