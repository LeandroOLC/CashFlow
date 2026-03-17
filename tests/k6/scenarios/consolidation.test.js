/**
 * ── Teste de Carga: Consolidation API ──────────────────────────────────────
 *
 * Requisito:
 *   "Em dias de pico, o consolidado recebe 50 req/s com no máximo 5% de perda."
 *
 * Este teste valida exatamente esse SLA usando constant-arrival-rate de 50 req/s.
 * O threshold de 5% de erro é o contrato do requisito.
 *
 * Cenários:
 *   1. smoke           — 1 VU / 30s: sanity check
 *   2. sla_peak        — 50 req/s / 3min: VALIDAÇÃO DO REQUISITO (cenário principal)
 *   3. above_sla       — 75 req/s / 1min: valida comportamento acima do SLA
 *   4. soak            — 20 VUs / 10min: estabilidade
 *   5. resilience      — testa que o serviço se recupera após sobrecarga
 *
 * Execução:
 *   k6 run tests/k6/scenarios/consolidation.test.js
 *
 *   # Só o cenário de SLA (principal):
 *   k6 run --env ONLY_SLA=true tests/k6/scenarios/consolidation.test.js
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';
import { BASE_URLS, authHeaders } from '../helpers/config.js';
import { ensureTestUser, getToken } from '../helpers/auth.js';

// ── Métricas customizadas ─────────────────────────────────────────────────────
const consolidationErrors  = new Counter('consolidation_errors_total');
const consolidationSuccess = new Counter('consolidation_success_total');
const consolidationLoss    = new Rate('consolidation_loss_rate');
const latestDuration       = new Trend('consolidation_latest_duration_ms', true);
const periodDuration       = new Trend('consolidation_period_duration_ms', true);

// ── Thresholds — baseados no requisito de 5% de perda máxima ─────────────────
export const options = {
  thresholds: {
    // REQUISITO: máx 5% de perda em 50 req/s
    http_req_failed:                               ['rate<0.05'],
    consolidation_loss_rate:                       ['rate<0.05'],

    // Performance: P95 < 500ms, P99 < 1000ms
    http_req_duration:                             ['p(95)<500', 'p(99)<1000'],
    'http_req_duration{endpoint:latest}':          ['p(95)<300'],
    'http_req_duration{endpoint:period}':          ['p(95)<500'],
    'http_req_duration{endpoint:by-date}':         ['p(95)<300'],
  },

  scenarios: {
    // ── 1. Smoke ──────────────────────────────────────────────────────────────
    smoke: {
      executor: 'constant-vus',
      vus: 1,
      duration: '30s',
      tags: { scenario: 'smoke' },
      exec: 'consolidationFlow',
    },

    // ── 2. SLA Peak — CENÁRIO PRINCIPAL DO REQUISITO ──────────────────────────
    // "50 req/s com máximo 5% de perda"
    // Usa arrival-rate para forçar exatamente 50 req/s independente da latência
    sla_peak: {
      executor: 'constant-arrival-rate',
      rate: 50,               // REQUISITO: 50 req/s
      timeUnit: '1s',
      duration: '3m',         // 3 minutos de carga sustentada
      preAllocatedVUs: 60,
      maxVUs: 150,
      tags: { scenario: 'sla_peak' },
      exec: 'consolidationFlow',
    },

    // ── 3. Acima do SLA — valida comportamento de degradação ──────────────────
    above_sla: {
      executor: 'constant-arrival-rate',
      rate: 75,               // 50% acima do requisito
      timeUnit: '1s',
      duration: '1m',
      preAllocatedVUs: 80,
      maxVUs: 200,
      tags: { scenario: 'above_sla' },
      exec: 'consolidationFlow',
    },

    // ── 4. Soak — estabilidade em execução longa ──────────────────────────────
    soak: {
      executor: 'constant-vus',
      vus: 20,
      duration: '10m',
      tags: { scenario: 'soak' },
      exec: 'consolidationFlow',
    },

    // ── 5. Resilience — recuperação após sobrecarga ───────────────────────────
    // Simula: sobrecarga → queda → recuperação
    resilience: {
      executor: 'ramping-arrival-rate',
      startRate: 10,
      timeUnit: '1s',
      stages: [
        { duration: '30s', target: 10  }, // baseline
        { duration: '30s', target: 100 }, // sobrecarga (2x acima do SLA)
        { duration: '30s', target: 10  }, // recuperação
        { duration: '1m',  target: 50  }, // volta ao SLA normal
      ],
      preAllocatedVUs: 100,
      maxVUs: 200,
      tags: { scenario: 'resilience' },
      exec: 'consolidationFlow',
    },
  },
};

// ── Setup ─────────────────────────────────────────────────────────────────────
export function setup() {
  ensureTestUser();
  const token = getToken();
  console.log(`Consolidation API: ${BASE_URLS.consolidation}`);
  console.log(`SLA alvo: 50 req/s com <= 5% perda`);
  return { token };
}

// ── Helpers de data ───────────────────────────────────────────────────────────
function toDateStr(date) {
  return date.toISOString().split('T')[0];
}

function randomPastDate(daysBack = 30) {
  const d = new Date();
  d.setDate(d.getDate() - Math.floor(Math.random() * daysBack));
  return toDateStr(d);
}

// ── Cenário principal: mix realista de consultas ──────────────────────────────
export function consolidationFlow(data) {
  const token = data.token;
  const headers = authHeaders(token);

  // Distribuição realista de uso:
  // 50% latest, 30% period, 20% by-date
  const rand = Math.random();

  if (rand < 0.50) {
    // GET /latest — mais frequente (dashboards em tempo real)
    group('get-latest', () => {
      const start = Date.now();
      const res = http.get(
        `${BASE_URLS.consolidation}/api/v1/daily-consolidation/latest`,
        { headers, tags: { service: 'consolidation', endpoint: 'latest' } }
      );
      latestDuration.add(Date.now() - start);

      const ok = check(res, {
        'latest: status 200 ou 404': (r) => r.status === 200 || r.status === 404,
        'latest: resposta válida':    (r) => r.status !== 500,
        'latest: dentro do timeout': (r) => r.timings.duration < 500,
      });

      if (res.status === 200) consolidationSuccess.add(1);
      if (!ok || res.status >= 500) {
        consolidationErrors.add(1);
        consolidationLoss.add(true);
      } else {
        consolidationLoss.add(false);
      }
    });

  } else if (rand < 0.80) {
    // GET /period — consultas de relatório mensal
    group('get-period', () => {
      const end   = new Date();
      const start = new Date();
      start.setDate(start.getDate() - 30);

      const startStr = toDateStr(start);
      const endStr   = toDateStr(end);

      const t = Date.now();
      const res = http.get(
        `${BASE_URLS.consolidation}/api/v1/daily-consolidation/period` +
        `?startDate=${startStr}&endDate=${endStr}&page=1&pageSize=31`,
        { headers, tags: { service: 'consolidation', endpoint: 'period' } }
      );
      periodDuration.add(Date.now() - t);

      const ok = check(res, {
        'period: status 200':        (r) => r.status === 200,
        'period: tem items':         (r) => Array.isArray(r.json('data.items')),
        'period: dentro do timeout': (r) => r.timings.duration < 1000,
      });

      if (ok) consolidationSuccess.add(1);
      else {
        consolidationErrors.add(1);
        consolidationLoss.add(true);
        return;
      }
      consolidationLoss.add(false);
    });

  } else {
    // GET /{date} — consulta por data específica
    group('get-by-date', () => {
      const date = randomPastDate(30);
      const res = http.get(
        `${BASE_URLS.consolidation}/api/v1/daily-consolidation/${date}`,
        { headers, tags: { service: 'consolidation', endpoint: 'by-date' } }
      );

      const ok = check(res, {
        'by-date: status 200 ou 404': (r) => r.status === 200 || r.status === 404,
        'by-date: não é 500':         (r) => r.status !== 500,
      });

      if (res.status === 200) consolidationSuccess.add(1);
      if (!ok || res.status >= 500) {
        consolidationErrors.add(1);
        consolidationLoss.add(true);
      } else {
        consolidationLoss.add(false);
      }
    });
  }
}
