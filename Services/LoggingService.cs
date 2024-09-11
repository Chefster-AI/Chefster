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

public class LoggingService(IMongoClient mongoClient) : ILog
{
    private readonly IMongoClient _mongoClient = mongoClient;

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
            collection.InsertOne(log);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to save log: {e}");
        }
    }
}
