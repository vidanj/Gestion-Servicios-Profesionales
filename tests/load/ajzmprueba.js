import http from 'k6/http';
import { check, sleep } from 'k6';

// Prueba de carga — POST /api/Auth/login
// Proceso crítico del sistema: verifica la contraseña y emite el token.
// Ejecutar con la API en Production, en Release y con AUTH_RATE_LIMIT elevado.
// Ver docs/plan-pruebas-carga-k6.md, secciones 5 y 6.

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const USER = __ENV.USER || 'k6prueba@ejemplo.com';
const PASS = __ENV.PASS || 'Prueba123!';

export const options = {
  stages: [
    { duration: '30s', target: 5 }, // incremento gradual
    { duration: '1m', target: 5 }, // sostenimiento: el tramo que se reporta
    { duration: '30s', target: 0 }, // descenso
  ],
  thresholds: {
    http_req_duration: ['p(95)<5000'], // nivel de servicio acordado
  },
};

export default function () {
  const payload = JSON.stringify({ email: USER, password: PASS });
  const params = {
    headers: { 'Content-Type': 'application/json' },
    tags: { endpoint: 'auth_login' },
  };

  const res = http.post(`${BASE_URL}/api/Auth/login`, payload, params);

  check(res, {
    'responde 200': (r) => r.status === 200,
    'devuelve token': (r) => {
      try {
        return typeof r.json('token') === 'string' && r.json('token').length > 0;
      } catch {
        return false;
      }
    },
    // Guardia contra el limitador de tasa: un 429 se resuelve en el middleware
    // sin tocar la base de datos, por lo que produce un p95 falsamente bueno.
    // Si esta verificación falla, la medición no es válida: hay que levantar
    // la API con AUTH_RATE_LIMIT elevado.
    'no fue rechazado por rate limit': (r) => r.status !== 429,
  });

  sleep(1); // tiempo de reflexión entre iteraciones
}
