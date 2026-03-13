# Этап сборки
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем sln (если есть)
COPY Application.sln ./

# Копируем все csproj
COPY Application/Application.csproj Application/

# Restore
RUN dotnet restore Application/Application.csproj

# Копируем весь код
COPY . .

# Publish
RUN dotnet publish Application/Application.csproj -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 80

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Application.dll"]