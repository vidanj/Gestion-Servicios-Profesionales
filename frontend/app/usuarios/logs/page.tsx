"use client";

// Navegacion y componentes UI usados por la vista.
import NextLink from "next/link";
import {
  Badge,
  Box,
  Button,
  Card,
  CardBody,
  CardHeader,
  Container,
  HStack,
  Heading,
  Select,
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
  return (
    <Box py={{ base: 10, md: 16 }}>
      <Container maxW="container.xl">
        <Stack spacing="6">
          {/* Encabezado de la pagina y navegacion entre vistas */}
          <Stack spacing="2">
            <Heading size="lg">Logs de usuarios</Heading>
            <Text color="muted">
              Historial visual de actividades y cambios recientes.
            </Text>
            <HStack spacing="4" flexWrap="wrap">
              <Button as={NextLink} href="/usuarios" variant="ghost">
                CRUD
              </Button>
              <Button as={NextLink} href="/usuarios/registrados" variant="ghost">
                Usuarios registrados
              </Button>
              <Button as={NextLink} href="/usuarios/logs" colorScheme="purple">
                Logs de usuarios
              </Button>
            </HStack>
          </Stack>

          {/* Card con filtros simulados y listado de eventos */}
          <Card variant="outline">
            <CardHeader>
              <HStack spacing="4" justify="space-between" flexWrap="wrap">
                <Heading size="md">Filtros</Heading>
                {/* Selects de ejemplo para filtrar logs */}
                <HStack spacing="3">
                  <Select placeholder="Estado">
                    <option>exitoso</option>
                    <option>alerta</option>
                    <option>error</option>
                  </Select>
                  <Select placeholder="Usuario">
                    <option>Andrea Torres</option>
                    <option>Bruno Castillo</option>
                    <option>Camila Diaz</option>
                    <option>Diego Ruiz</option>
                  </Select>
                  <Select placeholder="Tipo de evento">
                    <option>Cambio de contrasenia</option>
                    <option>Actualizacion de rol</option>
                    <option>Actualizacion de perfil</option>
                    <option>Suspension</option>
                  </Select>
                </HStack>
              </HStack>
            </CardHeader>
            <CardBody>
              <Stack spacing="4">
                {/* Renderizado de cada log */}
                {logs.map((log) => (
                  <Box
                    key={log.id}
                    borderWidth="1px"
                    borderRadius="md"
                    p="4"
                  >
                    <HStack justify="space-between" align="flex-start">
                      <Stack spacing="1">
                        <HStack spacing="3" flexWrap="wrap">
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
            </CardBody>
          </Card>
        </Stack>
      </Container>
    </Box>
  );
}
