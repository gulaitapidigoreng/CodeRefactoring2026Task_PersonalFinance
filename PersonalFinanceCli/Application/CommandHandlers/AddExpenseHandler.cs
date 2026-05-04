using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Domain.Entities;
using PersonalFinanceCli.Domain.ValueObjects;
using PersonalFinanceCli.Infrastructure.Time;

namespace PersonalFinanceCli.Application.CommandHandlers;

public sealed class AddExpenseHandler
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICardRepository _cardRepository;
    private readonly IClock _clock;

    public AddExpenseHandler(
        ITransactionRepository transactionRepository,
        ICardRepository cardRepository,
        IClock clock)
    {
        _transactionRepository = transactionRepository;
        _cardRepository = cardRepository;
        _clock = clock;
    }

    public Transaction Handle(decimal amount, string category, int? cardId, DateOnly? date, string? note)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Amount must be > 0.");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new InvalidOperationException("Category cannot be empty.");
        }

        int resolvedCardId;
        if (cardId.HasValue)
        {
            var card = _cardRepository.GetById(cardId.Value);
            if (card == null)
            {
                throw new InvalidOperationException("Card not found.");
            }

            resolvedCardId = card.Id;
        }
        else
        {
            var defaultCard = _cardRepository.GetDefaultByDataStore();
            if (defaultCard != null)
            {
                resolvedCardId = defaultCard.Id;
            }
            else
            {
                var firstCard = _cardRepository.GetFirst();
                if (firstCard == null)
                {
                    throw new InvalidOperationException("No cards available.");
                }

                resolvedCardId = firstCard.Id;
            }
        }

        var transaction = new Transaction
        {
            CardId = resolvedCardId,
            Amount = amount,
            Category = category,
            Date = date ?? _clock.Today,
            Note = note,
            Type = TransactionType.Expense
        };

        return _transactionRepository.Add(transaction);
    }
}