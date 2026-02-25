using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using EducationContentService.Domain.Lessons;
using EducationContentService.Domain.Shared;

namespace EducationContentService.Core.Features.Lessons;

public interface ILessonsRepository
{
    Task<Result<Guid, Error>> AddAsync(Lesson lesson,  CancellationToken cancellationToken = default);
    
    Task<Result<Lesson, Error>> GetBy(
        Expression<Func<Lesson, bool>> predicate,
        CancellationToken cancellationToken = default);
}