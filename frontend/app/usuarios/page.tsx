"use client";

import { useEffect, useState } from "react";
// Navegacion y componentes UI de Next/Chakra usados en la vista.
import NextLink from "next/link";
import {
  Badge,
  Box,
  Button,
  ButtonGroup,
  Card,
  Container,
  Field,
  HStack,
  Heading,
  IconButton,
  Input,
  NativeSelect,
  Separator,
  SimpleGrid,
  Stack,
  Switch,
  Table,
  Text,
} from "@chakra-ui/react";
import { FiEdit2, FiLock, FiPlus, FiTrash2 } from "react-icons/fi";

// Datos mock para renderizar el listado de usuarios.
const users = [
  {
    id: "USR-1024",
    name: "Andrea Torres",
    email: "andrea.torres@empresa.com",
    username: "atorres",
    role: "admin",
    status: "activo",
    lastLogin: "2026-02-10 09:24",
  },
  {
    id: "USR-1025",
    name: "Bruno Castillo",
    email: "bruno.castillo@empresa.com",
    username: "bcastillo",
    role: "manager",
    status: "activo",
    lastLogin: "2026-02-12 17:45",
  },
  {
    id: "USR-1026",
    name: "Camila Diaz",
    email: "camila.diaz@empresa.com",
    username: "cdiaz",
    role: "usuario",
    status: "suspendido",
    lastLogin: "2026-01-28 11:05",
  },
  {
    id: "USR-1027",
    name: "Diego Ruiz",
    email: "diego.ruiz@empresa.com",
    username: "druiz",
    role: "soporte",
    status: "activo",
    lastLogin: "2026-02-13 08:12",
  },
];

// Colores por rol para los badges en la tabla.
const roleColor: Record<string, string> = {
  admin: "purple",
  manager: "blue",
  usuario: "gray",
  soporte: "green",
};

// Colores por estado para los badges de status.
const statusColor: Record<string, string> = {
  activo: "green",
  suspendido: "red",
  pendiente: "yellow",
};

// Pagina de ejemplo para CRUD de usuarios.
export default function UsersCrudPage() {
  const [mounted, setMounted] = useState(false);
  useEffect(() => setMounted(true), []);
  if (!mounted) return null;

  return (
    <Box py={{ base: 10, md: 16 }}>
      <Container maxW="container.xl">
        <Stack gap="6">
          {/* Encabezado de la pagina y navegacion entre vistas */}
          <Stack gap="2">
            <Heading size="lg">CRUD de usuarios</Heading>
            <Text color="muted">
              Crea, edita y administra usuarios. Vista solamente visual.
            </Text>
            <HStack gap="4" flexWrap="wrap">
              <Button asChild colorScheme="purple">
                <NextLink href="/usuarios">CRUD</NextLink>
              </Button>
              <Button asChild variant="ghost">
                <NextLink href="/usuarios/registrados">Usuarios registrados</NextLink>
              </Button>
              <Button asChild variant="ghost">
                <NextLink href="/usuarios/logs">Logs de usuarios</NextLink>
              </Button>
            </HStack>
          </Stack>

          {/* Layout principal: formulario + tabla */}
          <SimpleGrid columns={{ base: 1, lg: 3 }} gap="6">
            {/* Card con formulario para crear usuarios (solo visual) */}
            <Card.Root>
              <Card.Header>
                <Heading size="md">Nuevo usuario</Heading>
              </Card.Header>
              <Card.Body>
                <Stack gap="4">
                  {/* Campo: nombre */}
                  <Field.Root>
                    <Field.Label>Nombre completo</Field.Label>
                    <Input placeholder="Ej. Andrea Torres" />
                  </Field.Root>
                  {/* Campo: correo */}
                  <Field.Root>
                    <Field.Label>Correo</Field.Label>
                    <Input type="email" placeholder="correo@empresa.com" />
                  </Field.Root>
                  {/* Campo: username */}
                  <Field.Root>
                    <Field.Label>Usuario</Field.Label>
                    <Input placeholder="usuario" />
                  </Field.Root>
                  {/* Campo: rol */}
                  <Field.Root>
                    <Field.Label>Rol</Field.Label>
                    <NativeSelect.Root>
                      <NativeSelect.Field placeholder="Selecciona un rol">
                        <option>admin</option>
                        <option>manager</option>
                        <option>usuario</option>
                        <option>soporte</option>
                      </NativeSelect.Field>
                    </NativeSelect.Root>
                  </Field.Root>
                  {/* Toggle: estado activo */}
                  <HStack justify="space-between">
                    <Text>Activo</Text>
                    <Switch.Root defaultChecked>
                      <Switch.HiddenInput />
                      <Switch.Control>
                        <Switch.Thumb />
                      </Switch.Control>
                    </Switch.Root>
                  </HStack>
                  {/* Acciones principales del formulario */}
                  <ButtonGroup>
                    <Button colorScheme="purple">
                      <FiPlus /> Crear usuario
                    </Button>
                    <Button variant="outline">Limpiar</Button>
                  </ButtonGroup>
                </Stack>
              </Card.Body>
            </Card.Root>

            {/* Card con tabla de usuarios existentes */}
            <Card.Root gridColumn={{ lg: "span 2" }}>
              <Card.Header>
                <HStack justify="space-between">
                  <Heading size="md">Usuarios existentes</Heading>
                  <Button variant="outline" size="sm">
                    Exportar
                  </Button>
                </HStack>
              </Card.Header>
              <Card.Body>
                {/* Tabla de usuarios mock */}
                <Table.Root variant="outline" size="sm">
                  <Table.Header>
                    <Table.Row>
                      <Table.ColumnHeader>Usuario</Table.ColumnHeader>
                      <Table.ColumnHeader>Rol</Table.ColumnHeader>
                      <Table.ColumnHeader>Estado</Table.ColumnHeader>
                      <Table.ColumnHeader>Ultimo acceso</Table.ColumnHeader>
                      <Table.ColumnHeader textAlign="right">Acciones</Table.ColumnHeader>
                    </Table.Row>
                  </Table.Header>
                  <Table.Body>
                    {/* Renderizado por cada usuario */}
                    {users.map((user) => (
                      <Table.Row key={user.id}>
                        <Table.Cell>
                          <Stack gap="0">
                            <Text fontWeight="semibold">{user.name}</Text>
                            <Text fontSize="sm" color="muted">
                              {user.email}
                            </Text>
                          </Stack>
                        </Table.Cell>
                        <Table.Cell>
                          {/* Badge de rol */}
                          <Badge colorScheme={roleColor[user.role]}>
                            {user.role}
                          </Badge>
                        </Table.Cell>
                        <Table.Cell>
                          {/* Badge de estado */}
                          <Badge colorScheme={statusColor[user.status]}>
                            {user.status}
                          </Badge>
                        </Table.Cell>
                        <Table.Cell>{user.lastLogin}</Table.Cell>
                        <Table.Cell textAlign="right">
                          {/* Acciones rapidas (solo visuales) */}
                          <HStack justify="flex-end">
                            <IconButton aria-label="Editar" size="sm" variant="ghost">
                              <FiEdit2 />
                            </IconButton>
                            <IconButton aria-label="Reset" size="sm" variant="ghost">
                              <FiLock />
                            </IconButton>
                            <IconButton aria-label="Eliminar" size="sm" colorScheme="red" variant="ghost">
                              <FiTrash2 />
                            </IconButton>
                          </HStack>
                        </Table.Cell>
                      </Table.Row>
                    ))}
                  </Table.Body>
                </Table.Root>

                {/* Separador entre tabla y metricas */}
                <Separator my="6" />

                {/* Tarjetas de resumen rapido */}
                <SimpleGrid columns={{ base: 1, md: 3 }} gap="4">
                  <Card.Root variant="outline">
                    <Card.Body>
                      <Text fontSize="sm" color="muted">
                        Usuarios activos
                      </Text>
                      <Heading size="md">18</Heading>
                    </Card.Body>
                  </Card.Root>
                  <Card.Root variant="outline">
                    <Card.Body>
                      <Text fontSize="sm" color="muted">
                        Invitaciones pendientes
                      </Text>
                      <Heading size="md">4</Heading>
                    </Card.Body>
                  </Card.Root>
                  <Card.Root variant="outline">
                    <Card.Body>
                      <Text fontSize="sm" color="muted">
                        Roles personalizados
                      </Text>
                      <Heading size="md">3</Heading>
                    </Card.Body>
                  </Card.Root>
                </SimpleGrid>
              </Card.Body>
            </Card.Root>
          </SimpleGrid>
        </Stack>
      </Container>
    </Box>
  );
}
