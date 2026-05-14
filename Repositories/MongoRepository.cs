using System.Linq.Expressions;
using ModernPosSystem.Models;
using MongoDB.Driver;

namespace ModernPosSystem.Repositories;

public class MongoRepository<T>(IMongoDbContext context) : IRepository<T> where T : BaseEntity
{
    private readonly IMongoCollection<T> _collection = context.GetCollection<T>();

    public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null)
    {
        predicate ??= x => x.IsActive;
        return await _collection.Find(predicate).ToListAsync();
    }

    public async Task<T?> GetByIdAsync(string id) =>
        await _collection.Find(x => x.Id == id && x.IsActive).FirstOrDefaultAsync();

    public async Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate) =>
        await _collection.Find(predicate).FirstOrDefaultAsync();

    public async Task<List<T>> GetByIdsAsync(IEnumerable<string> ids)
    {
        var idList = ids.ToList();
        return await _collection.Find(x => idList.Contains(x.Id) && x.IsActive).ToListAsync();
    }

    public async Task AddAsync(T entity) => await _collection.InsertOneAsync(entity);

    public async Task AddRangeAsync(IEnumerable<T> entities) => await _collection.InsertManyAsync(entities);

    public async Task UpdateAsync(T entity) =>
        await _collection.ReplaceOneAsync(x => x.Id == entity.Id, entity);

    public async Task<long> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        predicate ??= x => x.IsActive;
        return await _collection.CountDocumentsAsync(predicate);
    }

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate) =>
        await _collection.Find(predicate).AnyAsync();

    public async Task SoftDeleteAsync(string id, string updatedBy)
    {
        var update = Builders<T>.Update
            .Set(x => x.IsActive, false)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Set(x => x.UpdatedBy, updatedBy);

        await _collection.UpdateOneAsync(x => x.Id == id, update);
    }
}
