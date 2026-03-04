namespace EducationContentService.Contracts.Lessons;

public record GetLessonsRequest(string? Search, bool? IsDeleted, int Page, int PageSize);