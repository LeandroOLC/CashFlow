/**
 * ── Suite Completa: Todos os serviços em paralelo ──────────────────────────
 *
 * Executa Auth + Transactions + Consolidation simultaneamente
 * para simular carga real com todos os serviços ativos.
 *
 * Requisito validado:
 *   - Transactions: disponibilidade > 99%, erro < 1%
 *   - Consolidation: 50 req/s com <= 5% perda
 *   - Auth: P95 < 500ms
 *
 * Execução:
 *   k6 run tests/k6/scenarios/full-suite.test.js
 *
 *   # Com saída HTML:
 *   k6 run --out json=results/full-suite.json tests/k6/scenarios/full-suite.test.js
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Rate, Counter } from 'k6/metrics';
import { BASE_URLS, authHeaders, jsonHeaders } from '../helpers/config.js';
import { ensureTestUser, getToken } from '../helpers/auth.js';

const overallErrorRate = new Rate('overall_error_rate');
const slaViolations    = new Counter('sla_violations');

export const options = {
  thresholds: {
    // SLAs globais
    overall_error_rate:                            ['rate<0.05'],
    http_req_failed:                               ['rate<0.05'],
    http_req_duration:                             ['p(95)<500'],

    // Transactions — serviço crítico
    'http_req_failed{service:transactions}':       ['rate<0.01'],
    'http_req_duration{service:transactions}':     ['p(95)<400'],

    // Consolidation — requisito 50 req/s 5% loss
    'http_req_failed{service:consolidation}':      ['rate<0.05'],
    'http_req_duration{service:consolidation}':    ['p(95)<500'],

    // Auth
    'http_req_failed{service:auth}':               ['rate<0.01'],
    'http_req_duration{service:auth}':             ['p(95)<500'],
  },

  scenarios: {
    // ── Auth — carga de logins simultâneos ────────────────────────────────────
    auth_load: {
      executor: 'constant-arrival-rate',
      rate: 5,
      timeUnit: '1s',
      duration: '3m',
      preAllocatedVUs: 10,
      maxVUs: 20,
      tags: { scenario: 'auth_load' },
      exec: 'authFlow',
    },

    // ── Transactions — carga normal + pico ────────────────────────────────────
    transactions_load: {
      executor: 'ramping-arrival-rate',
      startRate: 10,
      timeUnit: '1s',
      stages: [
        { duration: '30s', target: 20 },
        { duration: '1m',  target: 40 },
        { duration: '30s', target: 50 }, // pico
        { duration: '1m',  target: 20 },
      ],
      preAllocatedVUs: 60,
      maxVUs: 120,
      tags: { scenario: 'transactions_load' },
      exec: 'transactionsFlow',
    },

    // ── Consolidation — 50 req/s (requisito) ─────────────────────────────────
    consolidation_sla: {
      executor: 'constant-arrival-rate',
      rate: 50,        // REQUISITO: 50 req/s
      timeUnit: '1s',
      duration: '3m',
      preAllocatedVUs: 60,
      maxVUs: 150,
      tags: { scenario: 'consolidation_sla' },
      exec: 'consolidationFlow',
    },
  },
};

export function setup() {
  ensureTestUser();
  const token = getToken();
  console.log('Suite completa iniciada');
  console.log(`  Auth:          ${BASE_URLS.auth}`);
  console.log(`  Transactions:  ${BASE_URLS.transactions}`);
  console.log(`  Consolidation: ${BASE_URLS.consolidation}`);
  return { token };
}

// ── Auth Flow ─────────────────────────────────────────────────────────────────
export function authFlow(data) {
  const res = http.post(
    `${BASE_URLS.auth}/api/v1/auth/login`,
    JSON.stringify({ email: 'loadtest@cashflow.dev', password: 'LoadTest@123' }),
    { headers: jsonHeaders(), tags: { service: 'auth' } }
  );

  const ok = check(res, {
    'auth: login ok': (r) => r.status === 200,
  });
  overallErrorRate.add(!ok);
  if (!ok) slaViolations.add(1);
}

// ── Transactions Flow ─────────────────────────────────────────────────────────
export function transactionsFlow(data) {
  const headers = authHeaders(data.token);

  // 70% leitura, 30% escrita
  if (Math.random() < 0.70) {
    const res = http.get(
      `${BASE_URLS.transactions}/api/v1/transactions?page=1&pageSize=20`,
      { headers, tags: { service: 'transactions' } }
    );
    const ok = check(res, { 'tx: list ok': (r) => r.status === 200 });
    overallErrorRate.add(!ok);
    if (!ok) slaViolations.add(1);
  } else {
    const res = http.post(
      `${BASE_URLS.transactions}/api/v1/transactions`,
      JSON.stringify({
        amount:      parseFloat((Math.random() * 2000 + 50).toFixed(2)),
        type:        Math.random() > 0.4 ? 'Credit' : 'Debit',
        date:        new Date().toISOString(),
        description: `Suite test ${Date.now()}`,
        categoryId:  '10000000-0000-0000-0000-000000000001',
      }),
      { headers, tags: { service: 'transactions' } }
    );
    const ok = check(res, { 'tx: create ok': (r) => r.status === 201 });
    overallErrorRate.add(!ok);
    if (!ok) slaViolations.add(1);
  }
}

// ── Consolidation Flow ────────────────────────────────────────────────────────
export function consolidationFlow(data) {
  const headers = authHeaders(data.token);

  const rand = Math.random();
  let res;

  if (rand < 0.50) {
    res = http.get(
      `${BASE_URLS.consolidation}/api/v1/daily-consolidation/latest`,
      { headers, tags: { service: 'consolidation' } }
    );
  } else if (rand < 0.80) {
    const end   = new Date();
    const start = new Date();
    start.setDate(start.getDate() - 30);
    res = http.get(
      `${BASE_URLS.consolidation}/api/v1/daily-consolidation/period` +
      `?startDate=${start.toISOString().split('T')[0]}` +
      `&endDate=${end.toISOString().split('T')[0]}&page=1&pageSize=31`,
      { headers, tags: { service: 'consolidation' } }
    );
  } else {
    const d = new Date();
    d.setDate(d.getDate() - Math.floor(Math.random() * 30));
    res = http.get(
      `${BASE_URLS.consolidation}/api/v1/daily-consolidation/${d.toISOString().split('T')[0]}`,
      { headers, tags: { service: 'consolidation' } }
    );
  }

  const ok = check(res, {
    'cons: não é 500': (r) => r.status !== 500,
    'cons: resposta':  (r) => r.status === 200 || r.status === 404,
  });
  overallErrorRate.add(!ok);
  if (!ok) slaViolations.add(1);
}
