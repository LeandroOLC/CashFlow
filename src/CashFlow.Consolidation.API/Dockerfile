FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/CashFlow.Shared/CashFlow.Shared.csproj", "src/CashFlow.Shared/"]
COPY ["src/CashFlow.Consolidation.API/CashFlow.Consolidation.API.csproj", "src/CashFlow.Consolidation.API/"]
RUN dotnet restore "src/CashFlow.Consolidation.API/CashFlow.Consolidation.API.csproj"

COPY . .
WORKDIR "/src/src/CashFlow.Consolidation.API"
RUN dotnet build "CashFlow.Consolidation.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "CashFlow.Consolidation.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "CashFlow.Consolidation.API.dll"]
