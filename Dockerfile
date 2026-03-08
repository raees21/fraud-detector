FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["FraudEngine.API/FraudEngine.API.csproj", "FraudEngine.API/"]
COPY ["FraudEngine.Application/FraudEngine.Application.csproj", "FraudEngine.Application/"]
COPY ["FraudEngine.Domain/FraudEngine.Domain.csproj", "FraudEngine.Domain/"]
COPY ["FraudEngine.Infrastructure/FraudEngine.Infrastructure.csproj", "FraudEngine.Infrastructure/"]
RUN dotnet restore "FraudEngine.API/FraudEngine.API.csproj"

COPY . .
WORKDIR "/src/FraudEngine.API"
RUN dotnet build "FraudEngine.API.csproj" -c Release -o /app/build
RUN dotnet publish "FraudEngine.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0
RUN groupadd --gid 10001 appgroup \
    && useradd --uid 10001 --gid appgroup --create-home --shell /usr/sbin/nologin appuser
COPY --from=build /app/publish .
RUN chown -R appuser:appgroup /app
USER appuser
EXPOSE 8080
ENTRYPOINT ["dotnet", "FraudEngine.API.dll"]
