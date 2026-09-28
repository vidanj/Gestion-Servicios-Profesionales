import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 5 },
    { duration: '1m', target: 5 },
    { duration: '30s', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<5000'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

// Se loguea UNA sola vez antes de arrancar la carga, y reparte el token
// entre todos los usuarios virtuales (igual que hace el script de login).
export function setup() {
  const loginRes = http.post(
    `${BASE_URL}/api/Auth/login`,
    JSON.stringify({
      email: __ENV.TEST_EMAIL,
      password: __ENV.TEST_PASSWORD,
    }),
    { headers: { 'Content-Type': 'application/json' } }
  );

  check(loginRes, { 'login exitoso': (r) => r.status === 200 });

  const token = loginRes.json('token');
  return { token };
}

export default function (data) {
  const res = http.get(`${BASE_URL}/api/Auth/me`, {
    headers: { Authorization: `Bearer ${data.token}` },
  });

  check(res, {
    'responde 200': (r) => r.status === 200,
    'devuelve el email del usuario': (r) => r.json('email') !== undefined,
    'no fue rechazado por autorizacion': (r) => r.status !== 401 && r.status !== 403,
    'no fue rechazado por rate limit': (r) => r.status !== 429,
  });

  sleep(1);
}