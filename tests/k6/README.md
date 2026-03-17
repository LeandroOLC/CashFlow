# CashFlow — Testes de Carga com K6

## Requisitos validados

| Requisito | Teste | Threshold |
|-----------|-------|-----------|
| Lançamentos não ficam indisponíveis se consolidado cair | `resilience.test.js` | `transactions_available > 99%` |
| Consolidado suporta 50 req/s em pico | `consolidation.test.js` (cenário `sla_peak`) | `http_req_failed < 5%` |
| Consolidado aceita máx 5% perda | `consolidation.test.js` | `consolidation_loss_rate < 5%` |

---

## Instalação do K6

```bash
# Windows (Chocolatey)
choco install k6

# macOS
brew install k6

# Linux
sudo gpg -k
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg \
  --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" \
  | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update && sudo apt-get install k6

# Docker
docker pull grafana/k6
```

---

## Pré-requisitos

Antes de rodar os testes, os serviços precisam estar no ar:

```bash
# Via Docker Compose (recomendado para testes de carga)
docker-compose up -d

# Ou individualmente (desenvolvimento)
# Terminal 1: dotnet run --project src/CashFlow.Auth.API
# Terminal 2: dotnet run --project src/CashFlow.Transactions.API
# Terminal 3: dotnet run --project src/CashFlow.Consolidation.API
```

O usuário de teste é criado automaticamente pelo `setup()` de cada teste:
- Email: `loadtest@cashflow.dev`
- Senha: `LoadTest@123`

Se quiser usar credenciais diferentes:
```bash
k6 run --env TEST_EMAIL=outro@email.com --env TEST_PASSWORD=OutraSenha tests/k6/scenarios/auth.test.js
```

---

## Execução

### Testes individuais

```bash
# Auth API
k6 run tests/k6/scenarios/auth.test.js

# Transactions API
k6 run tests/k6/scenarios/transactions.test.js

# Consolidation API — inclui o cenário sla_peak de 50 req/s
k6 run tests/k6/scenarios/consolidation.test.js

# Resiliência — Transactions com Consolidation caído
k6 run tests/k6/scenarios/resilience.test.js

# Suite completa — todos os serviços em paralelo
k6 run tests/k6/scenarios/full-suite.test.js
```

### Apenas o cenário de SLA (50 req/s)

```bash
# Executa só o cenário sla_peak por 3 minutos
k6 run --env ONLY_SLA=true tests/k6/scenarios/consolidation.test.js
```

### Com URLs customizadas (Docker ou ambiente remoto)

```bash
k6 run \
  --env AUTH_URL=http://localhost:5001 \
  --env TRANSACTIONS_URL=http://localhost:5002 \
  --env CONSOLIDATION_URL=http://localhost:5003 \
  tests/k6/scenarios/full-suite.test.js
```

### Via Docker

```bash
docker run --rm -i \
  --network cashflow-network \
  -e AUTH_URL=http://cashflow-auth:8080 \
  -e TRANSACTIONS_URL=http://cashflow-transactions:8080 \
  -e CONSOLIDATION_URL=http://cashflow-consolidation:8080 \
  -v $(pwd)/tests/k6:/tests \
  grafana/k6 run /tests/scenarios/full-suite.test.js
```

---

## Cenários por teste

### `auth.test.js`
| Cenário | VUs | Duração | Objetivo |
|---------|-----|---------|----------|
| smoke | 1 | 30s | Sanity check |
| load | 0→20 | 2min | Uso normal |
| stress | 0→80 | 3min | Pico de logins |

### `transactions.test.js`
| Cenário | Config | Duração | Objetivo |
|---------|--------|---------|----------|
| smoke | 1 VU | 30s | Sanity check |
| load_normal | 0→25 VUs | 3min | Dia útil normal |
| load_peak | 50 req/s | 2min | Pico do requisito |
| spike | 5→100 req/s | 2min | Pico súbito |
| soak | 15 VUs | 10min | Estabilidade longa |

### `consolidation.test.js`
| Cenário | Config | Duração | Threshold |
|---------|--------|---------|-----------|
| smoke | 1 VU | 30s | — |
| **sla_peak** | **50 req/s** | **3min** | **≤ 5% erro** |
| above_sla | 75 req/s | 1min | documentação |
| soak | 20 VUs | 10min | estabilidade |
| resilience | rampa 10→100→50 | 2min | recuperação |

### `resilience.test.js`
| Fase | O que acontece | Esperado |
|------|----------------|---------|
| 0–70s | Todos os serviços no ar | Transactions OK, Consolidation OK |
| 70s+ | `docker stop cashflow-consolidation` | Transactions continua OK |
| Final | Relatório de disponibilidade | `transactions_available > 99%` |

---

## Saída dos resultados

```bash
# JSON para análise posterior
k6 run --out json=results/consolidation-$(date +%Y%m%d-%H%M).json \
  tests/k6/scenarios/consolidation.test.js

# CSV
k6 run --out csv=results/consolidation.csv \
  tests/k6/scenarios/consolidation.test.js
```

## Interpretando os thresholds

Ao final da execução, K6 mostra:

```
✓ http_req_failed............: 1.23%  < 5.00% ✓ PASSOU
✓ consolidation_loss_rate....: 2.10%  < 5.00% ✓ PASSOU
✗ http_req_duration..........: p(95)=612ms > 500ms ✗ FALHOU

FAIL — 1 threshold(s) violated
```

- **✓** = SLA atendido
- **✗** = SLA violado (ajuste a infraestrutura ou os parâmetros)
