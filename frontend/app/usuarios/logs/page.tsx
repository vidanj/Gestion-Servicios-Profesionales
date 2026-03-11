"use client";

import { useEffect, useState, useCallback } from "react";
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

const apiUrl = process.env.NEXT_PUBLIC_ALLOWED_PATH;

const statusColor: Record<string, string> = {
  "0": "green",
  "1": "orange",
  "2": "red",
};

const statusLabel: Record<string, string> = {
  "0": "exitoso",
  "1": "alerta",
  "2": "error",
};

const actionLabel: Record<string, string> = {
  "0": "Cambio de contraseña",
  "1": "Actualización de rol",
  "2": "Intento de acceso",
  "3": "Actualización de perfil",
  "4": "Exportación de reportes",
  "5": "Suspensión de usuario",
  "6": "Creación de usuario",
  "7": "Eliminación de usuario",
};

type LogRow = {
  id: string;
  userId: string;
  userName: string;
  action: number;
  detail?: string;
  status: number;
  createdAt: string;
};

type UserOption = {
  id: string;
  name: string;
};

const getToken = () =>
  typeof window !== "undefined" ? localStorage.getItem("token") : null;

const authHeaders = () => ({
  "Content-Type": "application/json",
  Authorization: `Bearer ${getToken()}`,
});

export default function UserLogsPage() {
  const [mounted, setMounted] = useState(false);
  const [logs, setLogs] = useState<LogRow[]>([]);
  const [users, setUsers] = useState<UserOption[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  // Filtros
  const [filterStatus, setFilterStatus] = useState("");
  const [filterUser, setFilterUser] = useState("");
  const [filterAction, setFilterAction] = useState("");

  const fetchUsers = useCallback(async () => {
    try {
      const res = await fetch(`${apiUrl}/api/Users?page=1&size=100`, {
        headers: authHeaders(),
      });
      if (!res.ok) return;
      const json = await res.json();
      const raw: { id: string; firstName: string; lastName: string }[] =
        json.data ?? json.Data ?? [];
      setUsers(raw.map((u) => ({ id: u.id, name: `${u.firstName} ${u.lastName}` })));
    } catch {
      console.error("Error cargando usuarios para filtro");
    }
  }, []);

  const fetchLogs = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const params = new URLSearchParams({ page: "1", size: "50" });
      if (filterStatus !== "") params.append("status", filterStatus);
      if (filterUser !== "") params.append("userId", filterUser);
      if (filterAction !== "") params.append("action", filterAction);

      const res = await fetch(`${apiUrl}/api/UserLogs?${params.toString()}`, {
        headers: authHeaders(),
      });

      if (!res.ok) throw new Error("Error al cargar logs");

      const json = await res.json();
      setLogs(json.data ?? json.Data ?? []);
      setTotal(json.total ?? json.Total ?? 0);
    } catch {
      setError("No se pudieron cargar los logs.");
    } finally {
      setLoading(false);
    }
  }, [filterStatus, filterUser, filterAction]);

  useEffect(() => { setMounted(true); }, []);
  useEffect(() => { if (mounted) { fetchUsers(); fetchLogs(); } }, [mounted, fetchUsers, fetchLogs]);

  if (!mounted) return null;

  return (
    <Box py={{ base: 10, md: 16 }}>
      <Container maxW="container.xl">
        <Stack gap="6">
          <Stack gap="2">
            <Heading size="lg">Logs de usuarios</Heading>
            <Text color="muted">Historial de actividades y cambios recientes.</Text>
            <HStack gap="4" flexWrap="wrap">
              <Button asChild variant="ghost">
                <NextLink href="/usuarios">CRUD</NextLink>
              </Button>
              <Button asChild variant="ghost">
                <NextLink href="/usuarios/registrados">Usuarios registrados</NextLink>
              </Button>
              <Button asChild colorPalette="purple">
                <NextLink href="/usuarios/logs">Logs de usuarios</NextLink>
              </Button>
            </HStack>
          </Stack>

          {error && (
            <Text data-testid="logs-error" color="red.500">{error}</Text>
          )}

          <Card.Root variant="outline">
            <Card.Header>
              <HStack gap="4" justify="space-between" flexWrap="wrap">
                <Heading size="md">
                  Filtros
                  {total > 0 && (
                    <Text as="span" fontSize="sm" color="muted" ml="2">
                      ({total} resultados)
                    </Text>
                  )}
                </Heading>
                <HStack gap="3" flexWrap="wrap">
                  <NativeSelect.Root>
                    <NativeSelect.Field
                      data-testid="filter-status"
                      value={filterStatus}
                      onChange={e => setFilterStatus(e.target.value)}
                    >
                      <option value="">Estado</option>
                      <option value="0">Exitoso</option>
                      <option value="1">Alerta</option>
                      <option value="2">Error</option>
                    </NativeSelect.Field>
                  </NativeSelect.Root>

                  <NativeSelect.Root>
                    <NativeSelect.Field
                      data-testid="filter-user"
                      value={filterUser}
                      onChange={e => setFilterUser(e.target.value)}
                    >
                      <option value="">Usuario</option>
                      {users.map(u => (
                        <option key={u.id} value={u.id}>{u.name}</option>
                      ))}
                    </NativeSelect.Field>
                  </NativeSelect.Root>

                  <NativeSelect.Root>
                    <NativeSelect.Field
                      data-testid="filter-action"
                      value={filterAction}
                      onChange={e => setFilterAction(e.target.value)}
                    >
                      <option value="">Tipo de evento</option>
                      {Object.entries(actionLabel).map(([val, label]) => (
                        <option key={val} value={val}>{label}</option>
                      ))}
                    </NativeSelect.Field>
                  </NativeSelect.Root>

                  <Button
                    data-testid="filter-clear"
                    variant="outline"
                    size="sm"
                    onClick={() => {
                      setFilterStatus("");
                      setFilterUser("");
                      setFilterAction("");
                    }}
                  >
                    Limpiar
                  </Button>
                </HStack>
              </HStack>
            </Card.Header>

            <Card.Body>
              {loading ? (
                <Text data-testid="logs-loading" textAlign="center" py="4">Cargando...</Text>
              ) : logs.length === 0 ? (
                <Text data-testid="logs-empty" color="muted" textAlign="center" py="4">
                  Sin logs registrados.
                </Text>
              ) : (
                <Stack gap="4">
                  {logs.map(log => (
                    <Box
                      key={log.id}
                      data-testid="log-row"
                      borderWidth="1px"
                      borderRadius="md"
                      p="4"
                    >
                      <HStack justify="space-between" align="flex-start">
                        <Stack gap="1">
                          <HStack gap="3" flexWrap="wrap">
                            <Badge colorPalette={statusColor[String(log.status)]}>
                              {statusLabel[String(log.status)]}
                            </Badge>
                            <Text fontWeight="semibold">
                              {actionLabel[String(log.action)] ?? log.action}
                            </Text>
                          </HStack>
                          {log.detail && <Text color="muted">{log.detail}</Text>}
                          <Text fontSize="sm" color="muted">{log.userName}</Text>
                        </Stack>
                        <Text fontSize="sm" color="muted">
                          {new Date(log.createdAt).toLocaleString("es-HN")}
                        </Text>
                      </HStack>
                    </Box>
                  ))}
                </Stack>
              )}
            </Card.Body>
          </Card.Root>
        </Stack>
      </Container>
    </Box>
  );
}
