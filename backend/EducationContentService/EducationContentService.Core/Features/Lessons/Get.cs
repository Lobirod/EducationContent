using Core.Validation;
using CSharpFunctionalExtensions;
using EducationContentService.Contracts.Lessons;
using EducationContentService.Core.Database;
using EducationContentService.Domain.Lessons;
using FluentValidation;
using FluentValidation.Results;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Shared.SharedKernel;

namespace EducationContentService.Core.Features.Lessons;

public class GetLessonRequestValidator : AbstractValidator<GetLessonsRequest>
{
    public GetLessonRequestValidator()
    {
        RuleFor(x => x.Search).MaximumLength(1000).WithError(GeneralErrors.ValueIsInvalid("search"));

        RuleFor(x => x.Page)
            .NotNull().WithError(GeneralErrors.ValueIsInvalid("page"))
            .GreaterThan(0).WithError(GeneralErrors.ValueIsInvalid("page"));

        RuleFor(x => x.PageSize)
            .NotNull().WithError(GeneralErrors.ValueIsInvalid("page"))
            .GreaterThan(0).WithError(GeneralErrors.ValueIsInvalid("page size"));
    }
}

public sealed class GetEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/lessons", async Task<EndpointResult<PaginationLessonResponse>>(
            [AsParameters] GetLessonsRequest request,
            [FromServices] GetHandler handler,
            CancellationToken cancellationToken) => await handler.Handle(request, cancellationToken));
    }
}

public sealed class GetHandler
{
    private readonly IEducationReadDbContext _readDbContext;
    private readonly IValidator<GetLessonsRequest> _validator;

    public GetHandler(
        IEducationReadDbContext readDbContext,
        IValidator<GetLessonsRequest> validator)
    {
        _readDbContext = readDbContext;
        _validator = validator;
    }

    public async Task<Result<PaginationLessonResponse, Error>> Handle(
        GetLessonsRequest request,
        CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        IQueryable<Lesson> query = _readDbContext.LessonsQuery.IgnoreQueryFilters();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(l => EF.Functions.Like(l.Title.Value, $"%{request.Search}%"));
        }

        /*if (request.IsDeleted.HasValue)
        {
            query = query.Where(l => l.IsDeleted == request.IsDeleted.Value);
        }*/

        int lessonsCount = await query.CountAsync(cancellationToken);

        List<LessonDto> lessons = await query
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new LessonDto
            {
                Id = l.Id,
                Title = l.Title.Value,
                Description = l.Description.Value,
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt,
            })
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        int totalPages = (int)Math.Ceiling(lessonsCount / (double)request.PageSize);

        return new PaginationLessonResponse(lessons, lessonsCount);
    }
}
