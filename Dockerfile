# ---------- Stage 1: build (full .NET SDK) ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first: this layer is cached until the .csproj changes, so code edits rebuild fast
COPY DonationApi.csproj ./
RUN dotnet restore

# Copy the source and publish a Release build
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Stage 2: runtime (small ASP.NET image, no SDK, no source code) ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

# Run as the image's built-in non-root user
USER $APP_UID

ENTRYPOINT ["dotnet", "DonationApi.dll"]
