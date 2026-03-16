# ── Etapa 1: Build ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/CashFlow.WebApp/CashFlow.WebApp.csproj", "src/CashFlow.WebApp/"]
RUN dotnet restore "src/CashFlow.WebApp/CashFlow.WebApp.csproj"

COPY . .
WORKDIR "/src/src/CashFlow.WebApp"

# Publish gera apenas arquivos estáticos em /app/publish/wwwroot
RUN dotnet publish "CashFlow.WebApp.csproj" -c Release -o /app/publish

# ── Etapa 2: nginx serve os arquivos estáticos ────────────────────────────────
FROM nginx:alpine AS final

# Remove config padrão do nginx
RUN rm /etc/nginx/conf.d/default.conf

# Copia os arquivos estáticos gerados pelo publish
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

# Copia a config do nginx
COPY src/CashFlow.WebApp/nginx.conf /etc/nginx/conf.d/cashflow.conf

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD wget -qO- http://localhost/ || exit 1
