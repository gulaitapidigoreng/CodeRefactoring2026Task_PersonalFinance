using PersonalFinanceCli.Domain.ValueObjects;

namespace PersonalFinanceCli.Domain.Entities;

public sealed class DailyLimit : EntityBase
{
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
}