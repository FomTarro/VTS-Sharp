using System;
using Microsoft.Extensions.Logging;
using VTS.Core;

namespace AdvancedPlugin.Services;

//Singleton

public class VTSLogger : IVTSLogger
{
    private readonly ILogger<VTSLogger> _logger;

    public VTSLogger(ILogger<VTSLogger> logger)
    {
        _logger = logger;
    }

    public void Log(string message) => _logger.LogInformation(message);

    public void LogError(string error) => _logger.LogError(error);

    public void LogError(Exception error) => _logger.LogError(error, error.Message);

    public void LogWarning(string warning) => _logger.LogWarning(warning);
}