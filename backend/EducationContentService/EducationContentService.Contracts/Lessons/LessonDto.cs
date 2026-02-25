namespace EducationContentService.Contracts.Lessons;

public record LessonDto
{
    public Guid Id { get; init; }
    
    public string Title { get; init; } = string.Empty;
    
    public string Description { get; init; } = string.Empty;
    
    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}