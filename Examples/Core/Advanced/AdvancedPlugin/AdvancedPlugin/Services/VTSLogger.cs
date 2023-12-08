using System;
using Microsoft.Extensions.Logging;
using VTS.Core;

namespace VTS.Core.Examples.Advanced.Services;

//Singleton

public class VTSLogger(ILogger<VTSLogger> logger) : IVTSLogger
{
    public void Log(string message) => logger.LogInformation(message);
    public void LogError(string error) => logger.LogError(error);
    public void LogError(Exception error) => logger.LogError(error, error?.Message);
    public void LogWarning(string warning) => logger.LogWarning(warning);
}