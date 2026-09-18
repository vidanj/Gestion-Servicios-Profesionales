import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
stages: [
    { duration: '30s', target: 5 },
    { duration: '1m',  target: 5 },
    { duration: '30s', target: 0 },
],
thresholds: {
    http_req_duration: ['p(95)<5000'],
},
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const USER = __ENV.USER || 'k6prueba@ejemplo.com';
const PASS = __ENV.PASS || 'Prueba123!';

export function setup() {
const loginRes = http.post(`${BASE_URL}/api/Auth/login`, JSON.stringify({
    email: USER,
    password: PASS,
}), {
    headers: { 'Content-Type': 'application/json' },
});

const token = loginRes.json('token');

const meRes = http.get(`${BASE_URL}/api/Auth/me`, {
    headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json',
    },
});

  // La propiedad devuelta por la API se llama 'userId'
const userId = meRes.json('userId');
return { token, userId };
}

export default function (data) {
const params = {
    headers: {
    'Authorization': `Bearer ${data.token}`,
    'Content-Type': 'application/json',
    },
};

const res = http.get(`${BASE_URL}/api/Users/${data.userId}`, params);

check(res, {
    'responde 200': (r) => r.status === 200,
    'devuelve usuario por id': (r) => r.body && r.body.includes(data.userId),
    'no fue rechazado por autorizacion': (r) => r.status !== 401 && r.status !== 403,
    'no fue rechazado por rate limit': (r) => r.status !== 429,
});

sleep(1);
}
