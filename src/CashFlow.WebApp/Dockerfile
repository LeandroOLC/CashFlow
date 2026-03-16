# ── Etapa 1: Build ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/CashFlow.BlazorWasm/CashFlow.BlazorWasm.csproj", "src/CashFlow.BlazorWasm/"]
RUN dotnet restore "src/CashFlow.BlazorWasm/CashFlow.BlazorWasm.csproj"

COPY . .
WORKDIR "/src/src/CashFlow.BlazorWasm"
RUN dotnet publish "CashFlow.BlazorWasm.csproj" -c Release -o /app/publish

# ── Etapa 2: Runtime — ASP.NET Core serve os arquivos estáticos ───────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:8080/ || exit 1

ENTRYPOINT ["dotnet", "CashFlow.BlazorWasm.dll"]