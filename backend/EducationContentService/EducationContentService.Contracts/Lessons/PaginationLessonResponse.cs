namespace EducationContentService.Contracts.Lessons;

public record PaginationLessonResponse(IReadOnlyList<LessonDto> Lessons, int TotalCount);