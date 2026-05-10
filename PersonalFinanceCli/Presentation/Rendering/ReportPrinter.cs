using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Domain.Services;
using PersonalFinanceCli.Domain.ValueObjects;
using System.Globalization;

namespace PersonalFinanceCli.Presentation.Rendering;

public sealed class ReportPrinter
{
    private readonly TextWriter _writer;
    private readonly ICardRepository _cardRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILimitRepository _limitRepository;

    public ReportPrinter(
        TextWriter writer,
        ICardRepository cardRepository,
        ITransactionRepository transactionRepository,
        ILimitRepository limitRepository)
    {
        _writer = writer;
        _cardRepository = cardRepository;
        _transactionRepository = transactionRepository;
        _limitRepository = limitRepository;
    }

    public void Print(DailyReport report)
    {
        _writer.WriteLine($"Date: {report.Date:yyyy-MM-dd}");
        _writer.WriteLine($"Income: {FormatMoney(report.Income, report.Currency)}");
        _writer.WriteLine($"Expense: {FormatMoney(report.Expense, report.Currency)}");

        // Refactored call
        PrintLimitFormatted(report.Expense, report.Limit?.Amount, report.Limit?.Currency ?? report.Currency, useFloor: true);

        var recalculatedCategories = RecalculateCategories(report.Date, report.Currency);

        _writer.WriteLine("By category:");
        foreach (var categoryPair in recalculatedCategories.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            _writer.WriteLine($"  {categoryPair.Key}: {FormatMoney(categoryPair.Value, report.Currency)}");
        }

        _writer.WriteLine("Cards:");
        foreach (var card in report.Cards.OrderBy(card => card.CardId))
        {
            var marker = card.IsDefault ? " (default)" : string.Empty;
            _writer.WriteLine($"  {card.CardName}{marker}: {FormatMoney(card.Balance, card.Currency)}");
        }
    }

    public void PrintDayUsingRepositories(DateOnly date)
    {
        var cards = _cardRepository.GetAll();
        var currency = cards.FirstOrDefault(card => card.IsDefault)?.Currency
            ?? cards.FirstOrDefault()?.Currency
            ?? Currency.RUB;

        var cardIds = cards.Where(card => card.Currency == currency).Select(card => card.Id).ToHashSet();
        var allTransactions = _transactionRepository.GetAll();

        decimal income = 0m;
        decimal expense = 0m;
        var byCategory = new Dictionary<string, decimal>();

        foreach (var transaction in allTransactions)
        {
            if (transaction.Date == date && cardIds.Contains(transaction.CardId))
            {
                if (transaction.Type == TransactionType.Income)
                {
                    income += transaction.Amount;
                }
                else
                {
                    expense += transaction.Amount;

                    if (byCategory.TryGetValue(transaction.Category, out var prev))
                    {
                        byCategory[transaction.Category] = prev + transaction.Amount;
                    }
                    else
                    {
                        byCategory[transaction.Category] = transaction.Amount;
                    }
                }
            }
        }

        var limit = _limitRepository.GetByDate(date);

        _writer.WriteLine($"Date: {date:yyyy-MM-dd}");
        _writer.WriteLine($"Income: {income:F2} {currency}");
        _writer.WriteLine($"Expense: {expense:F2} {currency}");

        // Refactored call
        PrintLimitFormatted(expense, limit?.Amount, limit?.Currency ?? currency, useFloor: false);

        _writer.WriteLine("By category:");
        foreach (var categoryPair in byCategory.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            _writer.WriteLine($"  {categoryPair.Key}: {categoryPair.Value:F2} {currency}");
        }

        _writer.WriteLine("Cards:");
        foreach (var card in cards.OrderBy(card => card.Id))
        {
            decimal balance = card.InitialBalance;

            foreach (var transaction in allTransactions)
            {
                if (transaction.CardId == card.Id)
                {
                    balance = transaction.Type == TransactionType.Income ? balance + transaction.Amount : balance - transaction.Amount;
                }
            }

            var defaultSuffix = card.IsDefault ? " (default)" : "";
            _writer.WriteLine($"  {card.Name}{defaultSuffix}: {balance:F2} {card.Currency}");
        }
    }

    // Refactored: Consolidated identical limit printing logic
    private void PrintLimitFormatted(decimal expense, decimal? limit, Currency currency, bool useFloor)
    {
        if (!limit.HasValue || limit.Value <= 0)
        {
            _writer.WriteLine("Limit: (not set)");
            return;
        }

        int percent;
        if (useFloor)
        {
            percent = (int)Math.Floor((expense / limit.Value) * 100m);
        }
        else
        {
            percent = limit.Value == 0m ? 0 : (int)Math.Round((expense / limit.Value) * 100m, MidpointRounding.AwayFromZero);
        }

        _writer.WriteLine($"Limit: {FormatMoney(limit.Value, currency)} ({percent}%)");
    }

    private Dictionary<string, decimal> RecalculateCategories(DateOnly date, Currency currency)
    {
        // ... (Remains the same) ...
        var cards = _cardRepository.GetAll();
        var cardIds = cards.Where(card => card.Currency == currency).Select(card => card.Id).ToHashSet();
        var byCategory = new Dictionary<string, decimal>(StringComparer.Ordinal);

        foreach (var transaction in _transactionRepository.GetAll())
        {
            if (transaction.Date != date || transaction.Type != TransactionType.Expense || !cardIds.Contains(transaction.CardId))
            {
                continue;
            }

            if (byCategory.TryGetValue(transaction.Category, out var prev))
            {
                byCategory[transaction.Category] = prev + transaction.Amount;
            }
            else
            {
                byCategory[transaction.Category] = transaction.Amount;
            }
        }

        return byCategory;
    }

    public static string FormatMoney(decimal amount, Currency currency)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{amount:F2} {currency}");
    }
}