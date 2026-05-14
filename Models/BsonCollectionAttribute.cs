using System.Reflection;

namespace ModernPosSystem.Models;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class BsonCollectionAttribute(string collectionName) : Attribute
{
    public string CollectionName { get; } = collectionName;

    public static string ResolveCollectionName<T>()
    {
        var attribute = typeof(T).GetCustomAttribute<BsonCollectionAttribute>();
        return attribute?.CollectionName ?? typeof(T).Name;
    }
}
