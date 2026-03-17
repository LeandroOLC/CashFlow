// ── Configuração central dos testes K6 ───────────────────────────────────────
export const BASE_URLS = {
  auth:          __ENV.AUTH_URL          || 'http://localhost:5001',
  transactions:  __ENV.TRANSACTIONS_URL  || 'http://localhost:5002',
  consolidation: __ENV.CONSOLIDATION_URL || 'http://localhost:5003',
};

// Credenciais para obter o JWT antes dos testes
export const TEST_USER = {
  email:    __ENV.TEST_EMAIL    || 'loadtest@cashflow.dev',
  password: __ENV.TEST_PASSWORD || 'LoadTest@123',
};

// ── Thresholds globais baseados nos requisitos ────────────────────────────────
// Requisito: 50 req/s com máximo 5% de perda
export const GLOBAL_THRESHOLDS = {
  // Taxa de erros HTTP <= 5%
  http_req_failed: ['rate<0.05'],

  // P95 das requisições <= 500ms (SLA de performance)
  http_req_duration: ['p(95)<500', 'p(99)<1000'],
};

// ── Thresholds por serviço ────────────────────────────────────────────────────
export const TRANSACTIONS_THRESHOLDS = {
  'http_req_failed{service:transactions}':   ['rate<0.01'],  // serviço crítico: < 1% erro
  'http_req_duration{service:transactions}': ['p(95)<400'],
};

export const CONSOLIDATION_THRESHOLDS = {
  'http_req_failed{service:consolidation}':   ['rate<0.05'],  // 5% conforme requisito
  'http_req_duration{service:consolidation}': ['p(95)<500'],
};

// ── Headers padrão ────────────────────────────────────────────────────────────
export function authHeaders(token) {
  return {
    'Content-Type':  'application/json',
    'Authorization': `Bearer ${token}`,
    'X-Correlation-Id': `k6-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
  };
}

export function jsonHeaders() {
  return { 'Content-Type': 'application/json' };
}
