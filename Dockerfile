# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore dependencies (layer cache when only app code changes)
COPY MuuqWear.Web/MuuqWear.Web.csproj MuuqWear.Web/
COPY MuuqWear.Application/MuuqWear.Application.csproj MuuqWear.Application/
COPY MuuqWear.Model/MuuqWear.Model.csproj MuuqWear.Model/
RUN dotnet restore MuuqWear.Web/MuuqWear.Web.csproj

COPY . .
RUN dotnet publish MuuqWear.Web/MuuqWear.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Default for local `docker run`; Render overrides via ASPNETCORE_URLS=http://+:${PORT}
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MuuqWear.Web.dll"]
