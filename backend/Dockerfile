FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY CoachOS.slnx .
COPY CoachOS.Domain/CoachOS.Domain.csproj CoachOS.Domain/
COPY CoachOS.Application/CoachOS.Application.csproj CoachOS.Application/
COPY CoachOS.Infrastructure/CoachOS.Infrastructure.csproj CoachOS.Infrastructure/
COPY CoachOS.API/CoachOS.API.csproj CoachOS.API/
COPY CoachOS.Tests/CoachOS.Tests.csproj CoachOS.Tests/
RUN dotnet restore CoachOS.slnx

# Copy everything and build
COPY . .
RUN dotnet publish CoachOS.API/CoachOS.API.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:5142
ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 5142

ENTRYPOINT ["dotnet", "CoachOS.API.dll"]
