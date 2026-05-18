FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    tesseract-ocr \
    tesseract-ocr-eng \
    poppler-utils \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/SummaryService.Api/SummaryService.Api.csproj", "src/SummaryService.Api/"]
COPY ["src/SummaryService.Application/SummaryService.Application.csproj", "src/SummaryService.Application/"]
COPY ["src/SummaryService.Domain/SummaryService.Domain.csproj", "src/SummaryService.Domain/"]
COPY ["src/SummaryService.Infrastructure/SummaryService.Infrastructure.csproj", "src/SummaryService.Infrastructure/"]
COPY ["src/SummaryService.Shared/SummaryService.Shared.csproj", "src/SummaryService.Shared/"]
RUN dotnet restore "src/SummaryService.Api/SummaryService.Api.csproj"

COPY . .
WORKDIR "/src/src/SummaryService.Api"
RUN dotnet build "SummaryService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SummaryService.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY Prompts/ ./Prompts/

ENTRYPOINT ["dotnet", "SummaryService.Api.dll"]
