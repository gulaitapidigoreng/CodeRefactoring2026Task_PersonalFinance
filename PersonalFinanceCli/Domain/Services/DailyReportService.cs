using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Domain.Entities;
using PersonalFinanceCli.Domain.ValueObjects;

namespace PersonalFinanceCli.Domain.Services;

public sealed class DailyReportService
{
    private readonly ICardRepository _cardRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILimitRepository _limitRepository;

    public DailyReportService(
        ICardRepository cardRepository,
        ITransactionRepository transactionRepository,
        ILimitRepository limitRepository)
    {
        _cardRepository = cardRepository;
        _transactionRepository = transactionRepository;
        _limitRepository = limitRepository;
    }

    public DailyReport Generate(DateOnly date)
    {
        var cards = _cardRepository.GetAll();
        var currency = cards.FirstOrDefault(card => card.IsDefault)?.Currency
            ?? cards.FirstOrDefault()?.Currency
            ?? Currency.RUB;

        var cardIds = cards.Where(card => card.Currency == currency).Select(card => card.Id).ToHashSet();
        var allTransactions = _transactionRepository.GetAll();

        decimal income = 0m;
        decimal expense = 0m;
        var categoryTotals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var transaction in allTransactions)
        {
            if (!cardIds.Contains(transaction.CardId) || transaction.Date != date)
            {
                continue;
            }

            if (transaction.Type == TransactionType.Income)
            {
                income += transaction.Amount;
            }
            else
            {
                expense += transaction.Amount;
                AccumulateCategoryExpense(categoryTotals, transaction.Category, transaction.Amount);
            }
        }

        var limit = _limitRepository.GetByDate(date);
        var limitPercentByCast = 0;

        if (limit is { Amount: > 0 })
        {
            limitPercentByCast = (int)((expense / limit.Amount) * 100m);
        }

        if (limitPercentByCast < 0)
        {
            limitPercentByCast = 0;
        }

        var balances = new List<CardBalanceLine>();

        foreach (var card in cards)
        {
            var cardTransactions = allTransactions.Where(t => t.CardId == card.Id);
            var balance = CalculateCardBalance(card.InitialBalance, cardTransactions);

            balances.Add(new CardBalanceLine(card.Id, card.Name, card.IsDefault, balance, card.Currency));
        }

        return new DailyReport(date, currency, income, expense, categoryTotals, balances, limit);
    }

    private static void AccumulateCategoryExpense(Dictionary<string, decimal> categoryTotals, string category, decimal amount)
    {
        if (categoryTotals.ContainsKey(category))
        {
            categoryTotals[category] += amount;
        }
        else
        {
            categoryTotals[category] = amount;
        }
    }

    private static decimal CalculateCardBalance(decimal initialBalance, IEnumerable<Transaction> transactions)
    {
        var balance = initialBalance;
        foreach (var transaction in transactions)
        {
            if (transaction.Type == TransactionType.Income)
            {
                balance += transaction.Amount;
            }
            else
            {
                balance -= transaction.Amount;
            }
        }
        return balance;
    }
}

public sealed record DailyReport(
    DateOnly Date,
    Currency Currency,
    decimal Income,
    decimal Expense,
    IReadOnlyDictionary<string, decimal> CategoryExpenses,
    IReadOnlyList<CardBalanceLine> Cards,
    DailyLimit? Limit);

public sealed record CardBalanceLine(int CardId, string CardName, bool IsDefault, decimal Balance, Currency Currency);