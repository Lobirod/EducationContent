namespace EducationContentService.Domain.ModuleItems;

public record ItemReference
{
    public Guid ItemId { get; }
    public ItemType Type { get; }

    private ItemReference(ItemType type, Guid itemId)
    {
        Type = type;
        ItemId = itemId;
    }

    public static ItemReference ToLesson(Guid lessonId) => new(ItemType.LESSON, lessonId);

    public static ItemReference ToIssue(Guid issueId) => new(ItemType.ISSUE, issueId);
}