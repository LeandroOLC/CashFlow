# CashFlow - Sistema de Fluxo de Caixa

Microsserviços ASP.NET 10 para controle de fluxo de caixa com lançamentos e consolidação diária.

## Arquitetura

```
CashFlow/
├── src/
│   ├── CashFlow.Shared/              # Código compartilhado
│   ├── CashFlow.Auth.API/            # Autenticação JWT (porta 5001)
│   ├── CashFlow.Transactions.API/    # Lançamentos (porta 5002)
│   └── CashFlow.Consolidation.API/  # Consolidado diário (porta 5003)
├── tests/
│   ├── CashFlow.Transactions.Tests/
│   └── CashFlow.Consolidation.Tests/
└── docker-compose.yml
```

## Stack
- **Framework**: ASP.NET 10
- **ORM**: Entity Framework Core 10 com FluentAPI
- **Banco de dados**: SQL Server 2022
- **Mensageria**: RabbitMQ 4
- **Autenticação**: JWT Bearer
- **Documentação**: Swagger/OpenAPI
- **Logs**: Serilog com Correlation ID
- **Testes**: xUnit + Moq + FluentAssertions

## Padrões Implementados
- ✅ Repository Pattern com Unit of Work
- ✅ SOLID em todas as camadas
- ✅ FluentAPI (zero DataAnnotations)
- ✅ Logging estrutural com Correlation ID
- ✅ Versionamento de API (/api/v1/...)
- ✅ Containerização com Docker multi-stage

## Executar com Docker

```bash
docker-compose up --build
```

## Endpoints

### Auth (http://localhost:5001)
| Método | Rota | Descrição |
|--------|------|-----------|
| POST | /api/v1/auth/register | Registrar usuário |
| POST | /api/v1/auth/login | Login e obter JWT |
| POST | /api/v1/auth/refresh-token | Renovar token |

### Transactions (http://localhost:5002) — requer JWT
| Método | Rota | Descrição |
|--------|------|-----------|
| POST | /api/v1/transactions | Criar lançamento |
| GET | /api/v1/transactions | Listar (paginado) |
| GET | /api/v1/transactions/{id} | Buscar por ID |
| PUT | /api/v1/transactions/{id} | Atualizar |
| DELETE | /api/v1/transactions/{id} | Remover |

### Consolidation (http://localhost:5003) — requer JWT
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | /api/v1/daily-consolidation/latest | Saldo mais recente |
| GET | /api/v1/daily-consolidation/{date} | Saldo por data |
| GET | /api/v1/daily-consolidation/period | Saldo por período |

## Swagger
- Auth: http://localhost:5001/swagger
- Transactions: http://localhost:5002/swagger
- Consolidation: http://localhost:5003/swagger
- RabbitMQ Management: http://localhost:15672 (guest/guest)

## Fluxo de dados
1. Usuário faz login → recebe JWT
2. POST /api/v1/transactions com JWT → cria lançamento
3. Transaction Service publica evento no RabbitMQ
4. Consolidation Service consome evento e atualiza saldo diário
5. GET /api/v1/daily-consolidation/latest → retorna saldo consolidado

## Executar testes
```bash
dotnet test
```
