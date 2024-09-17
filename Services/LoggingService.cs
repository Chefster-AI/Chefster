using Chefster.Common;
using Chefster.Interfaces;
using Chefster.Models;
using MongoDB.Driver;

namespace Chefster.Services;

/*
    Logging service that is used just for the Web App.
    Not accessible via an API
*/

public interface ILog
{
    public void Log(string message, LogLevels loglevel, string? source = null);
}

public class LoggingService(IMongoClient mongoClient, IConfiguration configuration) : ILog
{
    private readonly IMongoClient _mongoClient = mongoClient;
    private readonly IConfiguration _configuration = configuration;

    public void Log(string message, LogLevels loglevel, string? source = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            Console.WriteLine("Message cannot be empty!");
            return;
        }

        var database = _mongoClient.GetDatabase("chefster-logs");
        // if it doesn't exit it will create it
        var collection = database.GetCollection<LogModel>("logs");

        var log = new LogModel
        {
            Message = message,
            LogLevel = loglevel,
            Source = source,
            CreatedAt = DateTime.UtcNow.ToString()
        };

        try
        {
            // Print the log to console so we can use it for debugging
            Console.WriteLine(log.Message);
            // don't save logs from development
            if (_configuration["ASPNETCORE_ENVIRONMENT"] != "Development")
            {
                collection.InsertOne(log);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to save log: {e}");
        }
    }
}
