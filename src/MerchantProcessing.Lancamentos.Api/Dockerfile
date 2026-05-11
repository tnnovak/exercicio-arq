# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/MerchantProcessing.Lancamentos.Api/MerchantProcessing.Lancamentos.Api.csproj", "MerchantProcessing.Lancamentos.Api/"]
COPY ["src/MerchantProcessing.Domain/MerchantProcessing.Domain.csproj", "MerchantProcessing.Domain/"]
COPY ["src/MerchantProcessing.Infrastructure/MerchantProcessing.Infrastructure.csproj", "MerchantProcessing.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "MerchantProcessing.Lancamentos.Api/MerchantProcessing.Lancamentos.Api.csproj"

# Copy all source code
COPY src/ .

# Build
RUN dotnet build "MerchantProcessing.Lancamentos.Api/MerchantProcessing.Lancamentos.Api.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "MerchantProcessing.Lancamentos.Api/MerchantProcessing.Lancamentos.Api.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MerchantProcessing.Lancamentos.Api.dll"]
