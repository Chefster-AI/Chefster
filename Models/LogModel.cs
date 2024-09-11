using System.Text.Json.Serialization;
using Chefster.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Chefster.Models;

public class LogModel
{
    [BsonId]
    [JsonIgnore]
    public ObjectId Id { get; set; }
    public required LogLevels LogLevel { get; set; }
    public required string Message { get; set; }
    public string? Source { get; set; }
    public string? CreatedAt { get; set; }
}
