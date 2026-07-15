FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/DocBrief.Domain/DocBrief.Domain.csproj src/DocBrief.Domain/
COPY src/DocBrief.Application/DocBrief.Application.csproj src/DocBrief.Application/
COPY src/DocBrief.Infrastructure/DocBrief.Infrastructure.csproj src/DocBrief.Infrastructure/
COPY src/DocBrief.API/DocBrief.API.csproj src/DocBrief.API/
RUN dotnet restore src/DocBrief.API/DocBrief.API.csproj

COPY src/ src/
RUN dotnet publish src/DocBrief.API/DocBrief.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "DocBrief.API.dll"]
