using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Domain.Entities;
using PersonalFinanceCli.Domain.ValueObjects;

namespace PersonalFinanceCli.Infrastructure.Persistence;

public sealed class JsonLimitRepository : JsonRepositoryBase, ILimitRepository
{
    public JsonLimitRepository(JsonDataStore store) : base(store)
    {
    }

    public DailyLimit? GetByDate(DateOnly date)
    {
        return _store.Load().DailyLimits.FirstOrDefault(limit => limit.Date == date);
    }

    public DailyLimit Upsert(DateOnly date, decimal amount, Currency currency)
    {
        var data = _store.Load();
        var existing = data.DailyLimits.FirstOrDefault(limit => limit.Date == date);

        if (existing is null)
        {
            existing = new DailyLimit
            {
                Id = GenerateNextId(data.DailyLimits),
                Date = date,
                Amount = amount,
                Currency = currency
            };
            data.DailyLimits.Add(existing);
        }
        else
        {
            existing.Amount = amount;
            existing.Currency = currency;
        }

        _store.Save(data);
        return existing;
    }
}