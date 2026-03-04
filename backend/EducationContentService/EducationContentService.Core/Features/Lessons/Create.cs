using Core.Validation;
using CSharpFunctionalExtensions;
using EducationContentService.Contracts.Lessons;
using EducationContentService.Domain.Lessons;
using EducationContentService.Domain.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using Framework.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Shared.SharedKernel;

namespace EducationContentService.Core.Features.Lessons;

public class CreateLessonRequestValidator : AbstractValidator<CreateLessonRequest>
{
    public CreateLessonRequestValidator()
    {
        RuleFor(x => x.Title)
            .MustBeValueObject(Title.Create);

        RuleFor(x => x.Description)
            .MustBeValueObject(Description.Create);
    }
}

public class CreateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/lessons", async Task<EndpointResult<Guid>>(
                [FromBody] CreateLessonRequest request,
                [FromServices] CreateHandler handler,
                CancellationToken cancellationToken) => await handler.Handle(request, cancellationToken));
    }
}

public sealed class CreateHandler
{
    private readonly ILogger<CreateHandler> _logger;
    private readonly ILessonsRepository _lessonsRepository;
    private readonly IValidator<CreateLessonRequest> _validator;

    public CreateHandler(
        ILogger<CreateHandler> logger,
        ILessonsRepository lessonsRepository,
        IValidator<CreateLessonRequest> validator)
    {
        _logger = logger;
        _lessonsRepository = lessonsRepository;
        _validator = validator;
    }

    public async Task<Result<Guid, Error>> Handle(CreateLessonRequest request, CancellationToken cancellationToken)
    {
        ValidationResult validatorResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validatorResult.IsValid)
        {
            return validatorResult.ToError();
        }

        Title titleResult = Title.Create(request.Title).Value;

        Description descriptionResult = Description.Create(request.Description).Value;

        var lesson = new Lesson(Guid.NewGuid(), titleResult, descriptionResult, request.VideoId);

        Result<Guid, Error> result = await _lessonsRepository.AddAsync(lesson);
        if (result.IsFailure)
            return result.Error;

        _logger.LogInformation("Created lesson {Id}", lesson.Id);

        return lesson.Id;
    }
}