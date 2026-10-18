# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first so the package layer is cached between builds
COPY Decksy2.0/Decksy2.0.csproj Decksy2.0/
RUN dotnet restore Decksy2.0/Decksy2.0.csproj

COPY Decksy2.0/ Decksy2.0/
RUN dotnet publish Decksy2.0/Decksy2.0.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Folders for the SQLite database and the cookie encryption keys (volumes in docker-compose)
RUN mkdir -p /app/data /app/keys && chown app:app /app/data /app/keys

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

USER app
ENTRYPOINT ["dotnet", "Decksy2.0.dll"]
