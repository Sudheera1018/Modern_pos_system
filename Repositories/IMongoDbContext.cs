using MongoDB.Driver;

namespace ModernPosSystem.Repositories;

public interface IMongoDbContext
{
    IMongoDatabase Database { get; }
    IMongoCollection<T> GetCollection<T>();
}
