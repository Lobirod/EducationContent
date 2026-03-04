namespace EducationContentService.Contracts.Lessons;

public record MediaDto
{
    public Guid? Id { get; init; }
    
    public string? Url { get; init; } = string.Empty;
    
    public string Status { get; init; } = string.Empty;
}