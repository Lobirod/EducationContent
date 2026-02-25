namespace EducationContentService.Contracts.Lessons;

public record GetLessonsRequest(string? Search, int Page, int PageSize);