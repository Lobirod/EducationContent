using EducationContentService.Domain.ValueObjects;

namespace EducationContentService.Domain.Lessons;

public sealed class Lesson
{
    public Lesson(Guid? id, Title title, Description description)
    {
        Id = id ?? Guid.NewGuid();
        Title = title;
        Description = description;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
        IsDeleted = false;
        DeletedAt = null;
    }
    
    // EF Core
    private Lesson()
    {
    }

    public Guid Id { get; }

    public Title Title { get; private set; } = null!;

    public Description Description { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }
    
    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    public void UpdateInfo(Title title, Description description)
    {
        Title = title;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

}