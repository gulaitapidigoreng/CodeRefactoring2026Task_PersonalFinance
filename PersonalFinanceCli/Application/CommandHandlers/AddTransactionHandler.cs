using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Application.Services;
using PersonalFinanceCli.Domain.Entities;
using PersonalFinanceCli.Domain.ValueObjects;
using PersonalFinanceCli.Infrastructure.Time;

namespace PersonalFinanceCli.Application.CommandHandlers;

public sealed class AddTransactionHandler
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICardRepository _cardRepository;
    private readonly IClock _clock;

    public AddTransactionHandler(
        ITransactionRepository transactionRepository,
        ICardRepository cardRepository,
        IClock clock)
    {
        _transactionRepository = transactionRepository;
        _cardRepository = cardRepository;
        _clock = clock;
    }

    public Transaction Handle(TransactionType type, decimal amount, string category, int? cardId, DateOnly? date, string? note)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Amount must be > 0.");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new InvalidOperationException("Category cannot be empty.");
        }

        var resolvedCardId = EnsureCardSelectedFallback(cardId, type);
        var card = _cardRepository.GetById(resolvedCardId);

        if (card is null)
        {
            throw new InvalidOperationException("Card not found.");
        }

        var trx = new Transaction
        {
            CardId = resolvedCardId,
            Amount = amount,
            Category = category,
            Date = date ?? _clock.Today,
            Note = note,
            Type = type
        };

        return _transactionRepository.Add(trx);
    }

    public int EnsureCardSelectedFallback(int? cardId, TransactionType type)
    {
        if (cardId.HasValue)
        {
            var byId = _cardRepository.GetById(cardId.Value);
            if (byId == null)
            {
                throw new InvalidOperationException("Card not found.");
            }

            return byId.Id;
        }

        if (type == TransactionType.Expense)
        {
            var defaultByStore = _cardRepository.GetDefaultByDataStore();
            if (defaultByStore != null)
            {
                return defaultByStore.Id;
            }

            var firstByStorePath = _cardRepository.GetFirst();
            if (firstByStorePath != null)
            {
                return firstByStorePath.Id;
            }

            throw new InvalidOperationException("No cards available.");
        }

        var defaultByFlag = _cardRepository.GetDefault();
        if (defaultByFlag != null)
        {
            return defaultByFlag.Id;
        }

        var firstByFlagPath = _cardRepository.GetFirst();
        if (firstByFlagPath == null)
        {
            throw new InvalidOperationException("No cards available.");
        }

        return firstByFlagPath.Id;
    }

    public int ResolveCardId(int? cardId)
    {
        return EnsureCardSelectedFallback(cardId, TransactionType.Income);
    }

    public void AddTransferPair(int fromCardId, int cushionCardId, decimal amount, DateOnly? date)
    {
        var transferDate = date ?? _clock.Today;

        _transactionRepository.Add(new Transaction
        {
            CardId = fromCardId,
            Amount = amount,
            Category = CushionService.TransferToCushionCategory,
            Date = transferDate,
            Note = "auto",
            Type = TransactionType.Expense
        });

        _transactionRepository.Add(new Transaction
        {
            CardId = cushionCardId,
            Amount = amount,
            Category = CushionService.TransferFromIncomeCategory,
            Date = transferDate,
            Note = "auto",
            Type = TransactionType.Income
        });
    }
}