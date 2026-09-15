import http from 'k6/http';
import { check, sleep } from 'k6';

// Prueba de carga — GET /api/Users
// Lectura mas frecuente del sistema: consulta paginada que filtra por Status y
// proyecta la pagina a DTO. El endpoint exige rol Admin.
// Ejecutar con la API en Release contra la base local, nunca contra Render.
// Ver docs/plan-pruebas-carga-k6.md, secciones 5, 6, 7 y 8.

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const USER = __ENV.USER;
const PASS = __ENV.PASS;

// La pagina se parametriza para poder contrastar el costo de una pagina chica
// contra una grande sin tocar el script. La evidencia de la entrega se genera
// con los valores por defecto, que son los del propio endpoint.
const PAGE = __ENV.PAGE || '1';
const SIZE = __ENV.SIZE || '10';

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

// Un unico inicio de sesion por ejecucion: el token se reparte entre todos los
// usuarios virtuales, de modo que el costo de autenticarse se paga una vez y no
// una vez por iteracion (plan, seccion 6). Ademas evita que las peticiones de
// login choquen con el limitador de tasa de /api/Auth.
export function setup() {
  if (!USER || !PASS) {
    throw new Error(
      'Faltan credenciales: ejecutar con -e USER=<correo admin> -e PASS=<clave>.'
    );
  }

  const res = http.post(
    `${BASE_URL}/api/Auth/login`,
    JSON.stringify({ email: USER, password: PASS }),
    { headers: { 'Content-Type': 'application/json' }, tags: { endpoint: 'setup_login' } }
  );

  if (res.status !== 200) {
    throw new Error(`El inicio de sesion devolvio ${res.status}; sin token no hay medicion.`);
  }

  const token = res.json('token');
  if (!token) {
    throw new Error('El inicio de sesion no devolvio token.');
  }

  return { token };
}

export default function (data) {
  const params = {
    headers: { Authorization: `Bearer ${data.token}` },
    tags: { endpoint: 'users_list' },
  };

  const res = http.get(`${BASE_URL}/api/Users?page=${PAGE}&size=${SIZE}`, params);

  check(res, {
    'responde 200': (r) => r.status === 200,
    'devuelve la pagina': (r) => {
      try {
        const cuerpo = r.json();
        return typeof cuerpo.total === 'number' && Array.isArray(cuerpo.data);
      } catch {
        return false;
      }
    },
    // Guardias contra mediciones falsamente buenas. Un 401, un 403 o un 429 se
    // resuelven en el middleware sin consultar la base de datos ni proyectar la
    // pagina, de modo que darian un p95 excelente sobre una medicion invalida.
    // Si alguna de estas verificaciones falla, el resultado no se reporta.
    'no fue rechazado por autorizacion': (r) => r.status !== 401 && r.status !== 403,
    'no fue rechazado por rate limit': (r) => r.status !== 429,
  });

  sleep(1); // tiempo de reflexion entre iteraciones
}
