using PersonalFinanceCli.Application.Repositories;

namespace PersonalFinanceCli.Application.CommandHandlers;

public sealed class SetDefaultCardHandler : CardHandlerBase
{
    public SetDefaultCardHandler(ICardRepository cardRepository) : base(cardRepository)
    {
    }

    public void Handle(int cardId)
    {
        var card = _cardRepository.GetById(cardId);
        if (card is null)
        {
            throw new InvalidOperationException("Card not found.");
        }

        _cardRepository.SetDefault(cardId);
    }
}