# Build-Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SEW04_Projekt_Bsteh.csproj", "."]
RUN dotnet restore "./SEW04_Projekt_Bsteh.csproj"
COPY . .
RUN dotnet publish "./SEW04_Projekt_Bsteh.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime-Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Datenordner fuer SQLite
RUN mkdir -p /app/data

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "SEW04_Projekt_Bsteh.dll"]