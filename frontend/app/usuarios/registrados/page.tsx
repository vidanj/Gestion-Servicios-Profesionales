"use client";

import { useEffect, useState } from "react";
// Navegacion y componentes UI usados por la vista.
import NextLink from "next/link";
import {
  Avatar,
  Badge,
  Box,
  Button,
  Card,
  Container,
  HStack,
  Heading,
  SimpleGrid,
  Stack,
  Tag,
  Text,
} from "@chakra-ui/react";

// Datos mock de usuarios registrados con permisos.
const users = [
  {
    id: "USR-1024",
    name: "Andrea Torres",
    role: "admin",
    team: "Operacion",
    email: "andrea.torres@empresa.com",
    permissions: ["usuarios:editar", "reportes:ver", "roles:gestionar"],
  },
  {
    id: "USR-1025",
    name: "Bruno Castillo",
    role: "manager",
    team: "Ventas",
    email: "bruno.castillo@empresa.com",
    permissions: ["clientes:ver", "servicios:editar", "reportes:ver"],
  },
  {
    id: "USR-1026",
    name: "Camila Diaz",
    role: "usuario",
    team: "Legal",
    email: "camila.diaz@empresa.com",
    permissions: ["tickets:crear", "perfil:editar"],
  },
  {
    id: "USR-1027",
    name: "Diego Ruiz",
    role: "soporte",
    team: "Soporte",
    email: "diego.ruiz@empresa.com",
    permissions: ["tickets:ver", "tickets:cerrar", "usuarios:ver"],
  },
  {
    id: "USR-1028",
    name: "Elena Suarez",
    role: "auditor",
    team: "Compliance",
    email: "elena.suarez@empresa.com",
    permissions: ["logs:ver", "reportes:ver"],
  },
];

// Colores por rol para diferenciar visualmente.
const roleColor: Record<string, string> = {
  admin: "purple",
  manager: "blue",
  usuario: "gray",
  soporte: "green",
  auditor: "orange",
};

// Pagina de listado de usuarios con permisos.
export default function RegisteredUsersPage() {
  const [mounted, setMounted] = useState(false);
  useEffect(() => setMounted(true), []);
  if (!mounted) return null;

  return (
    <Box py={{ base: 10, md: 16 }}>
      <Container maxW="container.xl">
        <Stack gap="6">
          {/* Encabezado de la pagina y navegacion entre vistas */}
          <Stack gap="2">
            <Heading size="lg">Usuarios registrados</Heading>
            <Text color="muted">
              Lista visual de usuarios con roles y permisos.
            </Text>
            <HStack gap="4" flexWrap="wrap">
              <Button asChild variant="ghost">
                <NextLink href="/usuarios">CRUD</NextLink>
              </Button>
              <Button asChild colorPalette="purple">
                <NextLink href="/usuarios/registrados">Usuarios registrados</NextLink>
              </Button>
              <Button asChild variant="ghost">
                <NextLink href="/usuarios/logs">Logs de usuarios</NextLink>
              </Button>
            </HStack>
          </Stack>

          {/* Grid de tarjetas por usuario */}
          <SimpleGrid columns={{ base: 1, md: 2, xl: 3 }} gap="6">
            {/* Renderizado de cada card con datos mock */}
            {users.map((user) => (
              <Card.Root key={user.id} variant="outline">
                <Card.Header>
                  <HStack gap="4">
                    <Avatar.Root>
                      <Avatar.Fallback name={user.name} />
                    </Avatar.Root>
                    <Stack gap="1">
                      <Heading size="sm">{user.name}</Heading>
                      <Text fontSize="sm" color="muted">
                        {user.email}
                      </Text>
                    </Stack>
                    {/* Badge con el rol del usuario */}
                    <Badge colorPalette={roleColor[user.role]} ml="auto">
                      {user.role}
                    </Badge>
                  </HStack>
                </Card.Header>
                <Card.Body>
                  <Stack gap="3">
                    <Text fontSize="sm" color="muted">
                      Equipo: {user.team}
                    </Text>
                    <Stack gap="2">
                      <Text fontSize="sm" fontWeight="semibold">
                        Permisos
                      </Text>
                      {/* Lista visual de permisos */}
                      <HStack gap="2" flexWrap="wrap">
                        {user.permissions.map((permission) => (
                          <Tag.Root key={permission} size="sm" variant="subtle" colorPalette="purple">
                            <Tag.Label>{permission}</Tag.Label>
                          </Tag.Root>
                        ))}
                      </HStack>
                    </Stack>
                  </Stack>
                </Card.Body>
              </Card.Root>
            ))}
          </SimpleGrid>
        </Stack>
      </Container>
    </Box>
  );
}
