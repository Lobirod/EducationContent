FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 9001

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["EducationContentService.Web/EducationContentService.Web.csproj", "EducationContentService.Web/"]
COPY ["EducationContentService.Domain/EducationContentService.Domain.csproj", "EducationContentService.Domain/"]
COPY ["EducationContentService.Core/EducationContentService.Core.csproj", "EducationContentService.Core/"]
COPY ["EducationContentService.Infrastructure.Postgres/EducationContentService.Infrastructure.Postgres.csproj", "EducationContentService.Infrastructure.Postgres/"]

COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

RUN dotnet restore "EducationContentService.Web/EducationContentService.Web.csproj"
COPY . .
WORKDIR "/src/EducationContentService.Web"
RUN dotnet build "./EducationContentService.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./EducationContentService.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EducationContentService.Web.dll"]
