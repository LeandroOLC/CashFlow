/**
 * ── Teste de Resiliência: Transactions independente do Consolidation ─────────
 *
 * Requisito crítico:
 *   "O serviço de lançamentos NÃO deve ficar indisponível se o consolidado cair."
 *
 * Este teste valida exatamente esse requisito:
 *   1. Inicia carga normal em Transactions
 *   2. Simula a queda do Consolidation (chamadas intencionais que devem falhar)
 *   3. Valida que Transactions continua funcionando com < 1% de erro
 *   4. Confirma que o Consolidation caído não afeta o fluxo de lançamentos
 *
 * Como simular a queda do Consolidation:
 *   - Pare manualmente: docker stop cashflow-consolidation
 *   - Este teste tentará chamar o Consolidation e documentará as falhas
 *   - Mas valida que o Transactions permanece healthy
 *
 * Execução:
 *   # 1. Inicie todos os serviços
 *   docker-compose up -d
 *
 *   # 2. Rode o teste (em outro terminal, pare o consolidation durante o teste)
 *   k6 run tests/k6/scenarios/resilience.test.js
 *
 *   # 3. Durante o cenário "transactions_under_pressure", pare o consolidation:
 *   docker stop cashflow-consolidation
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';
import { BASE_URLS, authHeaders } from '../helpers/config.js';
import { ensureTestUser, getToken } from '../helpers/auth.js';

// ── Métricas ──────────────────────────────────────────────────────────────────
const txAvailable          = new Rate('transactions_available');
const txErrorsDuringOutage = new Counter('tx_errors_during_consolidation_outage');
const txSuccessDuringOutage = new Counter('tx_success_during_consolidation_outage');
const consolidationDown    = new Counter('consolidation_unavailable_count');

export const options = {
  thresholds: {
    // REQUISITO PRINCIPAL: Transactions deve manter < 1% erro mesmo com Consolidation caído
    'transactions_available':                   ['rate>0.99'],
    'http_req_failed{service:transactions}':    ['rate<0.01'],

    // Consolidation pode falhar (está simulando queda) — threshold relaxado
    'http_req_failed{service:consolidation}':   ['rate<1.00'], // aceitamos qualquer falha

    // Performance de Transactions não pode degradar
    'http_req_duration{service:transactions}':  ['p(95)<500'],
  },

  scenarios: {
    // ── 1. Baseline: ambos os serviços rodando ────────────────────────────────
    baseline: {
      executor: 'constant-vus',
      vus: 10,
      duration: '1m',
      tags: { scenario: 'baseline' },
      exec: 'baselineCheck',
    },

    // ── 2. Transactions sob pressão (consolidation pode estar caído) ──────────
    // É aqui que você para o cashflow-consolidation para simular a falha
    transactions_under_pressure: {
      executor: 'constant-arrival-rate',
      rate: 30,
      timeUnit: '1s',
      duration: '3m',
      preAllocatedVUs: 40,
      maxVUs: 80,
      tags: { scenario: 'transactions_under_pressure' },
      exec: 'transactionsOnlyFlow',
      startTime: '1m10s', // começa após o baseline
    },

    // ── 3. Verifica que o Consolidation estava mesmo indisponível ─────────────
    consolidation_probe: {
      executor: 'constant-arrival-rate',
      rate: 5,
      timeUnit: '1s',
      duration: '3m',
      preAllocatedVUs: 10,
      maxVUs: 20,
      tags: { scenario: 'consolidation_probe' },
      exec: 'probeConsolidation',
      startTime: '1m10s',
    },
  },
};

export function setup() {
  ensureTestUser();
  const token = getToken();

  console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
  console.log('TESTE DE RESILIÊNCIA: Transactions independente do Consolidation');
  console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
  console.log('INSTRUÇÃO: Após 70s de execução, pare o Consolidation:');
  console.log('  docker stop cashflow-consolidation');
  console.log('O Transactions deve continuar funcionando sem erros.');
  console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');

  return { token };
}

// ── Cenário 1: Baseline — valida que tudo funciona antes da queda ─────────────
export function baselineCheck(data) {
  const headers = authHeaders(data.token);

  group('baseline-transactions', () => {
    const res = http.get(
      `${BASE_URLS.transactions}/api/v1/transactions?page=1&pageSize=10`,
      { headers, tags: { service: 'transactions', scenario: 'baseline' } }
    );
    check(res, { 'baseline-tx: ok': (r) => r.status === 200 });
  });

  group('baseline-consolidation', () => {
    const res = http.get(
      `${BASE_URLS.consolidation}/api/v1/daily-consolidation/latest`,
      { headers, tags: { service: 'consolidation', scenario: 'baseline' } }
    );
    check(res, { 'baseline-cons: ok': (r) => r.status === 200 || r.status === 404 });
  });

  sleep(0.5);
}

// ── Cenário 2: Transactions com Consolidation possivelmente caído ─────────────
export function transactionsOnlyFlow(data) {
  const headers = authHeaders(data.token);
  let allOk = true;

  // Leitura de lançamentos
  group('list-transactions', () => {
    const res = http.get(
      `${BASE_URLS.transactions}/api/v1/transactions?page=1&pageSize=20`,
      { headers, tags: { service: 'transactions' } }
    );
    const ok = check(res, {
      'tx-list: status 200':    (r) => r.status === 200,
      'tx-list: não é 500':     (r) => r.status !== 500,
      'tx-list: tem resposta':  (r) => r.body.length > 0,
    });
    if (!ok) allOk = false;
  });

  // Criação de lançamento — operação de escrita que NÃO depende do Consolidation
  // (o Consolidation atualiza via RabbitMQ de forma assíncrona)
  group('create-transaction', () => {
    const res = http.post(
      `${BASE_URLS.transactions}/api/v1/transactions`,
      JSON.stringify({
        amount:      Math.floor(Math.random() * 1000) + 50,
        type:        Math.random() > 0.5 ? 'Credit' : 'Debit',
        date:        new Date().toISOString(),
        description: `Resiliência teste ${Date.now()}`,
        categoryId:  '10000000-0000-0000-0000-000000000001',
      }),
      { headers, tags: { service: 'transactions' } }
    );
    const ok = check(res, {
      'tx-create: status 201': (r) => r.status === 201,
      'tx-create: tem id':     (r) => !!r.json('data.id'),
    });
    if (!ok) allOk = false;
  });

  // Registra disponibilidade
  txAvailable.add(allOk);
  if (allOk) txSuccessDuringOutage.add(1);
  else       txErrorsDuringOutage.add(1);
}

// ── Cenário 3: Probe do Consolidation — mede disponibilidade ─────────────────
export function probeConsolidation(data) {
  const headers = authHeaders(data.token);

  const res = http.get(
    `${BASE_URLS.consolidation}/api/v1/daily-consolidation/latest`,
    {
      headers,
      tags:    { service: 'consolidation' },
      timeout: '3s',  // timeout curto — não bloquear o probe
    }
  );

  const isDown = res.status === 0 || res.status >= 500 || res.error;

  if (isDown) {
    consolidationDown.add(1);
    console.log(`[PROBE] Consolidation INDISPONÍVEL — status: ${res.status}`);
  }

  check(res, {
    // Este check vai falhar quando o Consolidation cair — é esperado
    'consolidation: disponível': (r) => r.status === 200 || r.status === 404,
  });
}
