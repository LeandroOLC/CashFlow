/**
 * ── Teste de Carga: Auth API ────────────────────────────────────────────────
 *
 * Objetivo: validar que o endpoint de login suporta uso normal sem degradação.
 * O Auth não tem requisito de 50 req/s, mas deve responder em < 500ms no P95.
 *
 * Cenários:
 *   1. smoke      — 1 VU / 30s: verifica que tudo funciona
 *   2. load       — rampa até 20 VUs / 2min: uso normal
 *   3. stress     — rampa até 50 VUs / 3min: pico de logins simultâneos
 *
 * Execução:
 *   k6 run tests/k6/scenarios/auth.test.js
 *   k6 run -e SCENARIO=smoke tests/k6/scenarios/auth.test.js
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';
import { BASE_URLS, TEST_USER, jsonHeaders, authHeaders } from '../helpers/config.js';
import { ensureTestUser, getToken } from '../helpers/auth.js';

// ── Métricas customizadas ─────────────────────────────────────────────────────
const loginSuccess  = new Counter('auth_login_success');
const loginFailed   = new Counter('auth_login_failed');
const loginDuration = new Trend('auth_login_duration_ms', true);
const refreshSuccess = new Counter('auth_refresh_success');

// ── Thresholds ────────────────────────────────────────────────────────────────
export const options = {
  thresholds: {
    http_req_failed:                     ['rate<0.05'],
    http_req_duration:                   ['p(95)<500', 'p(99)<1000'],
    'http_req_duration{endpoint:login}': ['p(95)<400'],
    auth_login_success:                  ['count>0'],
  },

  scenarios: {
    smoke: {
      executor: 'constant-vus',
      vus: 1,
      duration: '30s',
      tags: { scenario: 'smoke' },
    },
    load: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 10 },
        { duration: '1m',  target: 20 },
        { duration: '30s', target: 0  },
      ],
      tags: { scenario: 'load' },
    },
    stress: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 20 },
        { duration: '1m',  target: 50 },
        { duration: '30s', target: 80 },
        { duration: '30s', target: 0  },
      ],
      tags: { scenario: 'stress' },
    },
  },
};

// ── Setup — garante que o usuário de teste existe ─────────────────────────────
export function setup() {
  ensureTestUser();
  console.log(`Auth API: ${BASE_URLS.auth}`);
}

// ── Função principal ──────────────────────────────────────────────────────────
export default function () {
  group('login', () => {
    const start = Date.now();
    const res = http.post(
      `${BASE_URLS.auth}/api/v1/auth/login`,
      JSON.stringify({ email: TEST_USER.email, password: TEST_USER.password }),
      { headers: jsonHeaders(), tags: { service: 'auth', endpoint: 'login' } }
    );
    loginDuration.add(Date.now() - start);

    const ok = check(res, {
      'login: status 200':          (r) => r.status === 200,
      'login: tem accessToken':     (r) => !!r.json('data.accessToken'),
      'login: tem refreshToken':    (r) => !!r.json('data.refreshToken'),
      'login: tem userId':          (r) => !!r.json('data.userId'),
    });

    if (ok) loginSuccess.add(1);
    else    loginFailed.add(1);

    // Testa refresh token com o token obtido
    if (res.status === 200) {
      const refreshToken = res.json('data.refreshToken');
      const token        = res.json('data.accessToken');

      group('refresh-token', () => {
        const refreshRes = http.post(
          `${BASE_URLS.auth}/api/v1/auth/refresh-token`,
          JSON.stringify({ refreshToken }),
          { headers: authHeaders(token), tags: { service: 'auth', endpoint: 'refresh' } }
        );

        check(refreshRes, {
          'refresh: status 200':      (r) => r.status === 200,
          'refresh: novo accessToken': (r) => !!r.json('data.accessToken'),
        }) && refreshSuccess.add(1);
      });
    }
  });

  sleep(1);
}
