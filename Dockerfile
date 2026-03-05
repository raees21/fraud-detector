FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["FraudEngine.API/FraudEngine.API.csproj", "FraudEngine.API/"]
COPY ["FraudEngine.Application/FraudEngine.Application.csproj", "FraudEngine.Application/"]
COPY ["FraudEngine.Domain/FraudEngine.Domain.csproj", "FraudEngine.Domain/"]
COPY ["FraudEngine.Infrastructure/FraudEngine.Infrastructure.csproj", "FraudEngine.Infrastructure/"]
COPY ["FraudEngine.Rules/FraudEngine.Rules.csproj", "FraudEngine.Rules/"]

RUN dotnet restore "FraudEngine.API/FraudEngine.API.csproj"

COPY . .
WORKDIR "/src/FraudEngine.API"
RUN dotnet build "FraudEngine.API.csproj" -c Release -o /app/build
RUN dotnet publish "FraudEngine.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FraudEngine.API.dll"]
