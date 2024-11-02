using Chefster.Services;

namespace Chefster.Common;

/*
* Allows us to use an instance of the logger class within the static ServiceResult class
*/

public static class ServiceProviderFactory
{
    public static IServiceProvider? ServiceProvider { get; set; }
}

public static class LoggingHelper
{
    private static LoggingService? _logger;
    public static LoggingService Logger
    {
        get
        {
            if (_logger == null)
            {
                _logger = GetLoggingService();
            }
            return _logger;
        }
    }

    private static LoggingService GetLoggingService()
    {
        // Grab the service scope so that we can grab the instance of LoggingService
        using var scope = ServiceProviderFactory.ServiceProvider!.CreateScope();
        var service = scope.ServiceProvider.GetService<LoggingService>();
        if (service == null)
        {
            throw new Exception("Logging Service was null in LoggingHelper");
        }
        return service;
    }
}
