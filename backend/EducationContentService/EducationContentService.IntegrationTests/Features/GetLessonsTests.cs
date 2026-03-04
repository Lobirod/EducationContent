using System.Net.Http.Json;
using System.Runtime.InteropServices.JavaScript;
using CSharpFunctionalExtensions;
using EducationContentService.Contracts.Lessons;
using EducationContentService.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;
using Shared.SharedKernel;

namespace EducationContentService.IntegrationTests.Features;

public class GetLessonsTests : EducationServiceTestsBase
{
    public GetLessonsTests(IntegrationTestsWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetLessons_Should_Return_Lessons()
    {
        // arrange
        CancellationToken cancellationToken = new CancellationTokenSource().Token;

        for (int i = 0; i < 3; i++)
        {
            var createLessonRequest = new CreateLessonRequest(
                $"Lesson {i + 1}",
                $"Description {i + 1}",
                Guid.NewGuid());

            HttpResponseMessage createLessonResponse = await AppHttpClient
                .PostAsJsonAsync("/lessons", createLessonRequest, cancellationToken);

            createLessonResponse.EnsureSuccessStatusCode();
        }

        var getLessonsRequest = new GetLessonsRequest(null, null, 1, 3);

        var queryParams = new Dictionary<string, string?>
        {
            {
                "page", getLessonsRequest.Page.ToString()
            },
            {
                "pageSize", getLessonsRequest.PageSize.ToString()
            },
            {
                "search", null
            },
        };

        string url = QueryHelpers.AddQueryString("lessons", queryParams);

        HttpResponseMessage startMultipartResponse = await AppHttpClient.GetAsync(url, cancellationToken);

        // act
        Result<PaginationLessonResponse, Error> lessonsResponse = await startMultipartResponse
            .HandleResponseAsync<PaginationLessonResponse>(cancellationToken);

        // assert
        Assert.True(lessonsResponse.IsSuccess);
        Assert.Equal(3, lessonsResponse.Value.Lessons.Count);
        Assert.All(lessonsResponse.Value.Lessons, lesson => Assert.NotNull(lesson.Video));
    }
}

