using CSharpFunctionalExtensions;
using EducationContentService.Domain.Shared;

namespace EducationContentService.Domain.ModuleItems;

public record Position
{
    public const decimal INITIAL_STEP = 1000m;

    private Position(ItemType itemType, decimal value)
    {
        ItemType = itemType;
        Value = value;
    }

    public decimal Value { get; }

    public ItemType ItemType { get; }

    public static Position First(ItemType itemType) => new(itemType, INITIAL_STEP);

    public static Result<Position, Error> Between(Position before, Position after)
    {
        if (before.ItemType != after.ItemType || before.Value >= after.Value)
        {
            return GeneralErrors.ValueIsInvalid("позиция");
        }

        return new Position(before.ItemType, (before.Value + after.Value) / 2);
    }

    public static Position After(Position previous) => new(previous.ItemType, previous.Value + INITIAL_STEP);
}