import { http, HttpResponse } from 'msw'

// JWT falso pero con estructura real (header.payload.signature)
const fakeToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiZW1haWwiOiJ0ZXN0QGVqZW1wbG8uY29tIiwicm9sZSI6MSwiaWF0IjoxNzAwMDAwMDAwfQ.fake-signature'

// Base de datos en memoria para simular registro/login
const users = [
    {
        id: 1,
        email: 'juan@ejemplo.com',
        firstName: 'Juan',
        lastName: 'Perez',
        role: 1,
        password: 'password123'
    }
]

export const handlers = [

  // POST /api/auth/register
  http.post('http://localhost:5000/api/auth/register', async ({ request }) => {
    const body = await request.json()
    const { email, password, firstName, lastName, role } = body

    // Validación básica
    if (!email || !password || !firstName || !lastName) {
      return HttpResponse.json(
        { message: 'Todos los campos son requeridos' },
        { status: 400 }
      )
    }

    if (users.find(u => u.email === email)) {
      return HttpResponse.json(
        { message: 'El email ya está registrado' },
        { status: 409 }
      )
    }

    const newUser = { id: users.length + 1, email, firstName, lastName, role }
    users.push({ ...newUser, password })

    return HttpResponse.json(
      { token: fakeToken, user: newUser },
      { status: 201 }
    )
  }),

  // POST /api/auth/login
  http.post('http://localhost:5000/api/auth/login', async ({ request }) => {
    const { email, password } = await request.json()

    const user = users.find(u => u.email === email && u.password === password)

    if (!user) {
      return HttpResponse.json(
        { message: 'Credenciales inválidas' },
        { status: 401 }
      )
    }

    const { password: _, ...userSafe } = user

    return HttpResponse.json(
      { token: fakeToken, user: userSafe },
      { status: 200 }
    )
  }),

  // GET /api/auth/me
  http.get('http://localhost:5000/api/auth/me', ({ request }) => {
    const authHeader = request.headers.get('Authorization')

    if (!authHeader || !authHeader.startsWith('Bearer ')) {
      return HttpResponse.json(
        { message: 'No autorizado' },
        { status: 401 }
      )
    }

    // Simula el usuario autenticado
    return HttpResponse.json({
      id: 1,
      email: 'test@ejemplo.com',
      firstName: 'Juan',
      lastName: 'Perez',
      role: 1
    }, { status: 200 })
  }),
]