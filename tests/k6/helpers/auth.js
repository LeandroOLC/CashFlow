import http from 'k6/http';
import { check, fail } from 'k6';
import { BASE_URLS, TEST_USER, jsonHeaders } from './config.js';

// ── Obtém token JWT (chamado no setup() dos testes) ───────────────────────────
export function getToken() {
  const res = http.post(
    `${BASE_URLS.auth}/api/v1/auth/login`,
    JSON.stringify({ email: TEST_USER.email, password: TEST_USER.password }),
    { headers: jsonHeaders(), tags: { service: 'auth' } }
  );

  const ok = check(res, {
    'login status 200': (r) => r.status === 200,
    'token presente':   (r) => r.json('data.accessToken') !== undefined,
  });

  if (!ok) {
    fail(`Falha no login: ${res.status} ${res.body}`);
  }

  return res.json('data.accessToken');
}

// ── Registra o usuário de teste se ainda não existir ─────────────────────────
export function ensureTestUser() {
  const res = http.post(
    `${BASE_URLS.auth}/api/v1/auth/register`,
    JSON.stringify({
      email:           TEST_USER.email,
      fullName:        'Load Test User',
      password:        TEST_USER.password,
      confirmPassword: TEST_USER.password,
    }),
    { headers: jsonHeaders(), tags: { service: 'auth' } }
  );

  // 201 = criado, 400 = já existe — ambos são aceitáveis no setup
  if (res.status !== 201 && res.status !== 400) {
    console.warn(`ensureTestUser: status inesperado ${res.status}`);
  }
}
