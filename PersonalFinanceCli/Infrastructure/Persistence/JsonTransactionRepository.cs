using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Domain.Entities;

namespace PersonalFinanceCli.Infrastructure.Persistence;

public sealed class JsonTransactionRepository : JsonRepositoryBase, ITransactionRepository
{
    public JsonTransactionRepository(JsonDataStore store) : base(store)
    {
    }

    public IReadOnlyList<Transaction> GetAll()
    {
        return _store.Load().Transactions.OrderBy(t => t.Id).ToList();
    }

    public Transaction Add(Transaction transaction)
    {
        var data = _store.Load();
        transaction.Id = GenerateNextId(data.Transactions);
        data.Transactions.Add(transaction);
        _store.Save(data);
        return transaction;
    }
}