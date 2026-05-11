# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/MerchantProcessing.Consolidator.Worker/MerchantProcessing.Consolidator.Worker.csproj", "MerchantProcessing.Consolidator.Worker/"]
COPY ["src/MerchantProcessing.Domain/MerchantProcessing.Domain.csproj", "MerchantProcessing.Domain/"]
COPY ["src/MerchantProcessing.Infrastructure/MerchantProcessing.Infrastructure.csproj", "MerchantProcessing.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "MerchantProcessing.Consolidator.Worker/MerchantProcessing.Consolidator.Worker.csproj"

# Copy all source code
COPY src/ .

# Build
RUN dotnet build "MerchantProcessing.Consolidator.Worker/MerchantProcessing.Consolidator.Worker.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "MerchantProcessing.Consolidator.Worker/MerchantProcessing.Consolidator.Worker.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MerchantProcessing.Consolidator.Worker.dll"]
