using PersonalFinanceCli.Domain.Entities;

namespace PersonalFinanceCli.Infrastructure.Persistence;

public abstract class JsonRepositoryBase
{
    protected readonly JsonDataStore _store;

    protected JsonRepositoryBase(JsonDataStore store)
    {
        _store = store;
    }

    // Extracted Method for common auto-increment ID logic
    protected static int GenerateNextId<T>(IReadOnlyCollection<T> collection) where T : EntityBase
    {
        return collection.Count == 0 ? 1 : collection.Max(e => e.Id) + 1;
    }
}