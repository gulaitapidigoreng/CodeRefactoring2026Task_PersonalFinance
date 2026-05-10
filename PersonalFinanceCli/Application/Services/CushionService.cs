using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Domain.Entities;
using PersonalFinanceCli.Domain.ValueObjects;

namespace PersonalFinanceCli.Application.Services;

public sealed class CushionService
{
    public const string TransferToCushionCategory = "Transfer to cushion";
    public const string TransferFromIncomeCategory = "Transfer from income";

    private readonly ICardRepository _cardRepository;

    public CushionService(ICardRepository cardRepository)
    {
        _cardRepository = cardRepository;
    }

    public Card? GetCushionCard()
    {
        var cards = _cardRepository.GetAll();

        var byFlag = cards.FirstOrDefault(card => card.IsCushion);
        if (byFlag != null) return byFlag;

        var exact = cards.FirstOrDefault(card => card.Name == "Financial cushion");
        if (exact != null) return exact;

        return cards.FirstOrDefault(card => card.Name.Contains("cushion", StringComparison.OrdinalIgnoreCase));
    }

    public Card CreateCushion(Currency currency)
    {
        var existingCushion = GetCushionCard();
        if (existingCushion != null)
        {
            return existingCushion;
        }

        return _cardRepository.Add(new Card
        {
            Name = "Financial cushion",
            Currency = currency,
            InitialBalance = 0m,
            IsDefault = false,
            IsCushion = true
        });
    }

    public decimal DefaultTransferAmount(decimal incomeAmount, string category)
    {
        var isSalaryCategory = category.Contains("Salary", StringComparison.OrdinalIgnoreCase);

        if (incomeAmount < 10m)
        {
            return 1m;
        }

        if (isSalaryCategory)
        {
            return Floor2(incomeAmount * 0.20m);
        }

        return Floor2(incomeAmount * 0.10m);
    }

    public static decimal Floor2(decimal value)
    {
        return Math.Floor(value * 100m) / 100m;
    }
}