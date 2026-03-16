FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/CashFlow.BlazorWasm/CashFlow.BlazorWasm.csproj", "src/CashFlow.BlazorWasm/"]
RUN dotnet restore "src/CashFlow.BlazorWasm/CashFlow.BlazorWasm.csproj"

COPY . .
WORKDIR "/src/src/CashFlow.BlazorWasm"
RUN dotnet publish "CashFlow.BlazorWasm.csproj" -c Release -o /app/publish

FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html
COPY --from=build /app/publish/wwwroot .
COPY src/CashFlow.BlazorWasm/nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
