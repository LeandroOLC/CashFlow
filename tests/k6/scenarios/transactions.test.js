/**
 * ── Teste de Carga: Transactions API ───────────────────────────────────────
 *
 * Requisito crítico:
 *   "O serviço de lançamentos NÃO deve ficar indisponível se o consolidado cair."
 *   → Transactions é serviço primário: threshold de erro < 1% (mais restritivo que os 5%)
 *
 * Cenários:
 *   1. smoke        — 1 VU / 30s: sanity check
 *   2. load_normal  — rampa até 25 VUs: carga normal de dia útil
 *   3. load_peak    — 50 req/s constante / 2min: pico de 50 req/s do requisito
 *   4. spike        — pico súbito de 100 VUs / 30s: testa resiliência
 *   5. soak         — 15 VUs / 10min: valida estabilidade em execução longa
 *
 * Execução:
 *   k6 run tests/k6/scenarios/transactions.test.js
 *   k6 run --env SCENARIO=peak tests/k6/scenarios/transactions.test.js
 *
 * Execução com Docker:
 *   k6 run --env AUTH_URL=http://localhost:5001 \
 *           --env TRANSACTIONS_URL=http://localhost:5002 \
 *           tests/k6/scenarios/transactions.test.js
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Counter, Rate, Trend, Gauge } from 'k6/metrics';
import { BASE_URLS, TRANSACTIONS_THRESHOLDS, authHeaders } from '../helpers/config.js';
import { ensureTestUser, getToken } from '../helpers/auth.js';

// ── Métricas customizadas ─────────────────────────────────────────────────────
const txCreated      = new Counter('tx_created_total');
const txCreateFailed = new Counter('tx_create_failed_total');
const txListDuration = new Trend('tx_list_duration_ms', true);
const txCreateDuration = new Trend('tx_create_duration_ms', true);
const txErrorRate    = new Rate('tx_error_rate');

// ── Thresholds baseados no requisito ─────────────────────────────────────────
export const options = {
  thresholds: {
    // Transactions é serviço crítico — erro < 1%
    http_req_failed:                            ['rate<0.01'],
    http_req_duration:                          ['p(95)<400', 'p(99)<800'],
    'http_req_duration{endpoint:list}':         ['p(95)<300'],
    'http_req_duration{endpoint:create}':       ['p(95)<500'],
    tx_error_rate:                              ['rate<0.01'],
  },

  scenarios: {
    // ── 1. Smoke — verifica funcionalidade básica ─────────────────────────────
    smoke: {
      executor: 'constant-vus',
      vus: 1,
      duration: '30s',
      tags: { scenario: 'smoke' },
      exec: 'fullCrudFlow',
    },

    // ── 2. Load normal — uso diário ───────────────────────────────────────────
    load_normal: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 10 },
        { duration: '2m',  target: 25 },
        { duration: '30s', target: 0  },
      ],
      tags: { scenario: 'load_normal' },
      exec: 'fullCrudFlow',
    },

    // ── 3. Peak — 50 req/s constante (requisito do consolidado) ──────────────
    // Usa arrival-rate para garantir exatamente 50 req/s
    load_peak: {
      executor: 'constant-arrival-rate',
      rate: 50,               // 50 requisições por segundo
      timeUnit: '1s',
      duration: '2m',
      preAllocatedVUs: 60,    // VUs pré-alocados para absorver a carga
      maxVUs: 120,            // máximo de VUs permitido
      tags: { scenario: 'peak' },
      exec: 'readHeavyFlow',  // pico = mais leituras que escritas
    },

    // ── 4. Spike — pico súbito ────────────────────────────────────────────────
    spike: {
      executor: 'ramping-arrival-rate',
      startRate: 5,
      timeUnit: '1s',
      stages: [
        { duration: '10s', target: 5   },
        { duration: '10s', target: 100 }, // spike instantâneo
        { duration: '30s', target: 100 },
        { duration: '10s', target: 5   },
      ],
      preAllocatedVUs: 100,
      maxVUs: 200,
      tags: { scenario: 'spike' },
      exec: 'readHeavyFlow',
    },

    // ── 5. Soak — estabilidade em execução longa ──────────────────────────────
    soak: {
      executor: 'constant-vus',
      vus: 15,
      duration: '10m',
      tags: { scenario: 'soak' },
      exec: 'fullCrudFlow',
    },
  },
};

// ── Setup — login e obtenção do token ─────────────────────────────────────────
export function setup() {
  ensureTestUser();
  const token = getToken();
  console.log(`Transactions API: ${BASE_URLS.transactions}`);
  console.log(`Token obtido: ${token ? 'OK' : 'FALHOU'}`);
  return { token };
}

// ── Dados de teste ────────────────────────────────────────────────────────────
const CATEGORIES = [
  '10000000-0000-0000-0000-000000000001', // Salário
  '10000000-0000-0000-0000-000000000005', // Alimentação
  '10000000-0000-0000-0000-000000000006', // Transporte
];

function randomTransaction() {
  const isCredit = Math.random() > 0.4;
  return {
    amount:      parseFloat((Math.random() * 5000 + 10).toFixed(2)),
    type:        isCredit ? 'Credit' : 'Debit',
    date:        new Date().toISOString(),
    description: `K6 Load Test ${isCredit ? 'Receita' : 'Despesa'} ${Date.now()}`,
    categoryId:  CATEGORIES[Math.floor(Math.random() * CATEGORIES.length)],
  };
}

// ── Cenário: CRUD completo ────────────────────────────────────────────────────
export function fullCrudFlow(data) {
  const token = data.token;
  const headers = authHeaders(token);
  let createdId = null;

  // 1. Listar lançamentos
  group('list-transactions', () => {
    const start = Date.now();
    const res = http.get(
      `${BASE_URLS.transactions}/api/v1/transactions?page=1&pageSize=20`,
      { headers, tags: { service: 'transactions', endpoint: 'list' } }
    );
    txListDuration.add(Date.now() - start);

    const ok = check(res, {
      'list: status 200':       (r) => r.status === 200,
      'list: tem items':        (r) => Array.isArray(r.json('data.items')),
      'list: tem totalCount':   (r) => r.json('data.totalCount') !== undefined,
    });
    txErrorRate.add(!ok);
  });

  sleep(0.1);

  // 2. Criar lançamento
  group('create-transaction', () => {
    const start = Date.now();
    const res = http.post(
      `${BASE_URLS.transactions}/api/v1/transactions`,
      JSON.stringify(randomTransaction()),
      { headers, tags: { service: 'transactions', endpoint: 'create' } }
    );
    txCreateDuration.add(Date.now() - start);

    const ok = check(res, {
      'create: status 201': (r) => r.status === 201,
      'create: tem id':     (r) => !!r.json('data.id'),
    });

    if (ok) {
      txCreated.add(1);
      createdId = res.json('data.id');
    } else {
      txCreateFailed.add(1);
    }
    txErrorRate.add(!ok);
  });

  sleep(0.1);

  // 3. Buscar por ID (se criou com sucesso)
  if (createdId) {
    group('get-by-id', () => {
      const res = http.get(
        `${BASE_URLS.transactions}/api/v1/transactions/${createdId}`,
        { headers, tags: { service: 'transactions', endpoint: 'get-by-id' } }
      );
      check(res, {
        'get-by-id: status 200': (r) => r.status === 200,
        'get-by-id: id correto': (r) => r.json('data.id') === createdId,
      });
    });

    sleep(0.1);

    // 4. Atualizar lançamento
    group('update-transaction', () => {
      const updated = randomTransaction();
      updated.description = `K6 Updated ${Date.now()}`;
      const res = http.put(
        `${BASE_URLS.transactions}/api/v1/transactions/${createdId}`,
        JSON.stringify(updated),
        { headers, tags: { service: 'transactions', endpoint: 'update' } }
      );
      check(res, {
        'update: status 200': (r) => r.status === 200,
      });
    });

    sleep(0.1);

    // 5. Deletar lançamento criado (cleanup)
    group('delete-transaction', () => {
      const res = http.del(
        `${BASE_URLS.transactions}/api/v1/transactions/${createdId}`,
        null,
        { headers, tags: { service: 'transactions', endpoint: 'delete' } }
      );
      check(res, {
        'delete: status 204': (r) => r.status === 204,
      });
    });
  }

  sleep(0.5);
}

// ── Cenário: Leitura intensiva (pico) ────────────────────────────────────────
export function readHeavyFlow(data) {
  const token = data.token;
  const headers = authHeaders(token);

  // Alterna entre diferentes queries para simular uso real
  const scenarios = [
    () => http.get(
      `${BASE_URLS.transactions}/api/v1/transactions?page=1&pageSize=20`,
      { headers, tags: { service: 'transactions', endpoint: 'list' } }
    ),
    () => http.get(
      `${BASE_URLS.transactions}/api/v1/transactions?page=1&pageSize=20&type=Credit`,
      { headers, tags: { service: 'transactions', endpoint: 'list-credit' } }
    ),
    () => http.get(
      `${BASE_URLS.transactions}/api/v1/transactions?page=1&pageSize=20&type=Debit`,
      { headers, tags: { service: 'transactions', endpoint: 'list-debit' } }
    ),
    () => http.get(
      `${BASE_URLS.transactions}/api/v1/transaction-categories`,
      { headers, tags: { service: 'transactions', endpoint: 'categories' } }
    ),
  ];

  const fn = scenarios[Math.floor(Math.random() * scenarios.length)];
  const res = fn();

  check(res, {
    'read: status 200':  (r) => r.status === 200,
    'read: tem data':    (r) => r.json('data') !== null,
  });
}
