FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props global.json ./
COPY src/backend/Makers.Domain/Makers.Domain.csproj src/backend/Makers.Domain/
COPY src/backend/Makers.Application/Makers.Application.csproj src/backend/Makers.Application/
COPY src/backend/Makers.Infrastructure/Makers.Infrastructure.csproj src/backend/Makers.Infrastructure/
COPY src/backend/Makers.Api/Makers.Api.csproj src/backend/Makers.Api/
RUN dotnet restore src/backend/Makers.Api/Makers.Api.csproj
COPY src/backend src/backend
RUN dotnet publish src/backend/Makers.Api/Makers.Api.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Makers.Api.dll"]
