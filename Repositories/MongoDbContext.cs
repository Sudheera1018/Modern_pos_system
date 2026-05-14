using Microsoft.Extensions.Options;
using ModernPosSystem.Configurations;
using ModernPosSystem.Models;
using MongoDB.Driver;

namespace ModernPosSystem.Repositories;

public class MongoDbContext(IOptions<MongoDbSettings> options) : IMongoDbContext
{
    private readonly IMongoDatabase _database = new MongoClient(options.Value.ConnectionString)
        .GetDatabase(options.Value.DatabaseName);

    public IMongoDatabase Database => _database;

    public IMongoCollection<T> GetCollection<T>() =>
        _database.GetCollection<T>(BsonCollectionAttribute.ResolveCollectionName<T>());
}
