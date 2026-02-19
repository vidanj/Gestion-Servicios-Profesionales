"use client";

import { useEffect, useState } from "react";
// Navegacion y componentes UI usados por la vista.
import NextLink from "next/link";
import {
  Badge,
  Box,
  Button,
  Card,
  Container,
  HStack,
  Heading,
  NativeSelect,
  Stack,
  Text,
} from "@chakra-ui/react";

// Datos mock de eventos de auditoria.
const logs = [
  {
    id: "LOG-9001",
    user: "Andrea Torres",
    action: "Cambio de contrasenia",
    detail: "Actualizo la contrasenia desde el panel.",
    date: "2026-02-13 09:15",
    status: "exitoso",
  },
  {
    id: "LOG-9002",
    user: "Bruno Castillo",
    action: "Actualizacion de rol",
    detail: "Paso de usuario a manager.",
    date: "2026-02-12 18:02",
    status: "exitoso",
  },
  {
    id: "LOG-9003",
    user: "Camila Diaz",
    action: "Intento de acceso",
    detail: "Intento fallido de inicio de sesion.",
    date: "2026-02-11 07:41",
    status: "alerta",
  },
  {
    id: "LOG-9004",
    user: "Diego Ruiz",
    action: "Actualizacion de perfil",
    detail: "Modifico nombre de usuario a druiz.",
    date: "2026-02-10 16:33",
    status: "exitoso",
  },
  {
    id: "LOG-9005",
    user: "Elena Suarez",
    action: "Exportacion de reportes",
    detail: "Descargo reporte mensual de accesos.",
    date: "2026-02-09 12:22",
    status: "exitoso",
  },
  {
    id: "LOG-9006",
    user: "Andrea Torres",
    action: "Suspension de usuario",
    detail: "Suspendio a c.diaz por 24 horas.",
    date: "2026-02-08 10:05",
    status: "alerta",
  },
];

// Colores por severidad del evento.
const statusColor: Record<string, string> = {
  exitoso: "green",
  alerta: "orange",
  error: "red",
};

// Pagina de logs de usuarios (solo visual).
export default function UserLogsPage() {
  const [mounted, setMounted] = useState(false);
  useEffect(() => setMounted(true), []);
  if (!mounted) return null;

  return (
    <Box py={{ base: 10, md: 16 }}>
      <Container maxW="container.xl">
        <Stack gap="6">
          {/* Encabezado de la pagina y navegacion entre vistas */}
          <Stack gap="2">
            <Heading size="lg">Logs de usuarios</Heading>
            <Text color="muted">
              Historial visual de actividades y cambios recientes.
            </Text>
            <HStack gap="4" flexWrap="wrap">
              <Button asChild variant="ghost">
                <NextLink href="/usuarios">CRUD</NextLink>
              </Button>
              <Button asChild variant="ghost">
                <NextLink href="/usuarios/registrados">Usuarios registrados</NextLink>
              </Button>
              <Button asChild colorScheme="purple">
                <NextLink href="/usuarios/logs">Logs de usuarios</NextLink>
              </Button>
            </HStack>
          </Stack>

          {/* Card con filtros simulados y listado de eventos */}
          <Card.Root variant="outline">
            <Card.Header>
              <HStack gap="4" justify="space-between" flexWrap="wrap">
                <Heading size="md">Filtros</Heading>
                {/* Selects de ejemplo para filtrar logs */}
                <HStack gap="3">
                  <NativeSelect.Root>
                    <NativeSelect.Field placeholder="Estado">
                      <option>exitoso</option>
                      <option>alerta</option>
                      <option>error</option>
                    </NativeSelect.Field>
                  </NativeSelect.Root>
                  <NativeSelect.Root>
                    <NativeSelect.Field placeholder="Usuario">
                      <option>Andrea Torres</option>
                      <option>Bruno Castillo</option>
                      <option>Camila Diaz</option>
                      <option>Diego Ruiz</option>
                    </NativeSelect.Field>
                  </NativeSelect.Root>
                  <NativeSelect.Root>
                    <NativeSelect.Field placeholder="Tipo de evento">
                      <option>Cambio de contrasenia</option>
                      <option>Actualizacion de rol</option>
                      <option>Actualizacion de perfil</option>
                      <option>Suspension</option>
                    </NativeSelect.Field>
                  </NativeSelect.Root>
                </HStack>
              </HStack>
            </Card.Header>
            <Card.Body>
              <Stack gap="4">
                {/* Renderizado de cada log */}
                {logs.map((log) => (
                  <Box
                    key={log.id}
                    borderWidth="1px"
                    borderRadius="md"
                    p="4"
                  >
                    <HStack justify="space-between" align="flex-start">
                      <Stack gap="1">
                        <HStack gap="3" flexWrap="wrap">
                          {/* Badge con severidad del evento */}
                          <Badge colorScheme={statusColor[log.status]}>
                            {log.status}
                          </Badge>
                          <Text fontWeight="semibold">{log.action}</Text>
                        </HStack>
                        <Text color="muted">{log.detail}</Text>
                        <Text fontSize="sm" color="muted">
                          {log.user}
                        </Text>
                      </Stack>
                      <Text fontSize="sm" color="muted">
                        {log.date}
                      </Text>
                    </HStack>
                  </Box>
                ))}
              </Stack>
            </Card.Body>
          </Card.Root>
        </Stack>
      </Container>
    </Box>
  );
}
