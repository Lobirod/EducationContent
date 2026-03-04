using Core.Validation;
using CSharpFunctionalExtensions;
using EducationContentService.Contracts.Lessons;
using EducationContentService.Core.Database;
using EducationContentService.Domain.Lessons;
using FileService.Contracts;
using FileService.Contracts.Dtos;
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
        app.MapGet("/lessons", async Task<EndpointResult<PaginationLessonResponse>> (
            [AsParameters] GetLessonsRequest request,
            [FromServices] GetHandler handler,
            CancellationToken cancellationToken) => await handler.Handle(request, cancellationToken));
    }
}

public sealed class GetHandler
{
    private readonly IEducationReadDbContext _readDbContext;
    private readonly IFileCommunicationService _fileCommunicationService;
    private readonly IValidator<GetLessonsRequest> _validator;

    public GetHandler(
        IEducationReadDbContext readDbContext,
        IFileCommunicationService fileCommunicationService,
        IValidator<GetLessonsRequest> validator)
    {
        _readDbContext = readDbContext;
        _fileCommunicationService = fileCommunicationService;
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

        if (request.IsDeleted.HasValue)
        {
            query = query.Where(l => l.IsDeleted == request.IsDeleted.Value);
        }

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
                Video = new MediaDto { Id = l.VideoId, },
            })
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        int totalPages = (int)Math.Ceiling(lessonsCount / (double)request.PageSize);

        IReadOnlyList<Guid> mediaAssetIds = lessons
            .Where(l => l.Video != null && l.Video.Id.HasValue)
            .Select(l => l.Video!.Id!.Value)
            .ToList();

        Result<GetMediaAssetsResponse, Error> mediaAssets = await _fileCommunicationService
            .GetMediaAssets(new GetMediaAssetsRequest(mediaAssetIds), cancellationToken);

        if (mediaAssets.IsFailure)
            return mediaAssets.Error;

        var mediaAssetsDict = mediaAssets.Value.MediaAssets.ToDictionary(x => x.Id, x => x);

        foreach (LessonDto lessonDto in lessons)
        {
            if (lessonDto.Video != null
                && lessonDto.Video.Id.HasValue
                && mediaAssetsDict.TryGetValue(lessonDto.Video.Id.Value, out GetMediaAssetsDto? mediaAsset))
            {
                lessonDto.Video = new MediaDto
                {
                    Id = mediaAsset.Id, Status = mediaAsset.Status, Url = mediaAsset.Url,
                };
            }
        }

        return new PaginationLessonResponse(lessons, lessonsCount);
    }
}