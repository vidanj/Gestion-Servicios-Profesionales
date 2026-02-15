"use client";

// Navegacion y componentes UI de Next/Chakra usados en la vista.
import NextLink from "next/link";
import {
  Badge,
  Box,
  Button,
  ButtonGroup,
  Card,
  CardBody,
  CardHeader,
  Container,
  Divider,
  FormControl,
  FormLabel,
  HStack,
  Heading,
  IconButton,
  Input,
  Select,
  SimpleGrid,
  Stack,
  Switch,
  Table,
  Tbody,
  Td,
  Text,
  Th,
  Thead,
  Tr,
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
  return (
    <Box py={{ base: 10, md: 16 }}>
      <Container maxW="container.xl">
        <Stack spacing="6">
          {/* Encabezado de la pagina y navegacion entre vistas */}
          <Stack spacing="2">
            <Heading size="lg">CRUD de usuarios</Heading>
            <Text color="muted">
              Crea, edita y administra usuarios. Vista solamente visual.
            </Text>
            <HStack spacing="4" flexWrap="wrap">
              <Button as={NextLink} href="/usuarios" colorScheme="purple">
                CRUD
              </Button>
              <Button as={NextLink} href="/usuarios/registrados" variant="ghost">
                Usuarios registrados
              </Button>
              <Button as={NextLink} href="/usuarios/logs" variant="ghost">
                Logs de usuarios
              </Button>
            </HStack>
          </Stack>

          {/* Layout principal: formulario + tabla */}
          <SimpleGrid columns={{ base: 1, lg: 3 }} spacing="6">
            {/* Card con formulario para crear usuarios (solo visual) */}
            <Card>
              <CardHeader>
                <Heading size="md">Nuevo usuario</Heading>
              </CardHeader>
              <CardBody>
                <Stack spacing="4">
                  {/* Campo: nombre */}
                  <FormControl>
                    <FormLabel>Nombre completo</FormLabel>
                    <Input placeholder="Ej. Andrea Torres" />
                  </FormControl>
                  {/* Campo: correo */}
                  <FormControl>
                    <FormLabel>Correo</FormLabel>
                    <Input type="email" placeholder="correo@empresa.com" />
                  </FormControl>
                  {/* Campo: username */}
                  <FormControl>
                    <FormLabel>Usuario</FormLabel>
                    <Input placeholder="usuario" />
                  </FormControl>
                  {/* Campo: rol */}
                  <FormControl>
                    <FormLabel>Rol</FormLabel>
                    <Select placeholder="Selecciona un rol">
                      <option>admin</option>
                      <option>manager</option>
                      <option>usuario</option>
                      <option>soporte</option>
                    </Select>
                  </FormControl>
                  {/* Toggle: estado activo */}
                  <HStack justify="space-between">
                    <FormLabel mb="0">Activo</FormLabel>
                    <Switch defaultChecked />
                  </HStack>
                  {/* Acciones principales del formulario */}
                  <ButtonGroup>
                    <Button leftIcon={<FiPlus />} colorScheme="purple">
                      Crear usuario
                    </Button>
                    <Button variant="outline">Limpiar</Button>
                  </ButtonGroup>
                </Stack>
              </CardBody>
            </Card>

            {/* Card con tabla de usuarios existentes */}
            <Card gridColumn={{ lg: "span 2" }}>
              <CardHeader>
                <HStack justify="space-between">
                  <Heading size="md">Usuarios existentes</Heading>
                  <Button variant="outline" size="sm">
                    Exportar
                  </Button>
                </HStack>
              </CardHeader>
              <CardBody>
                {/* Tabla de usuarios mock */}
                <Table variant="simple" size="sm">
                  <Thead>
                    <Tr>
                      <Th>Usuario</Th>
                      <Th>Rol</Th>
                      <Th>Estado</Th>
                      <Th>Ultimo acceso</Th>
                      <Th textAlign="right">Acciones</Th>
                    </Tr>
                  </Thead>
                  <Tbody>
                    {/* Renderizado por cada usuario */}
                    {users.map((user) => (
                      <Tr key={user.id}>
                        <Td>
                          <Stack spacing="0">
                            <Text fontWeight="semibold">{user.name}</Text>
                            <Text fontSize="sm" color="muted">
                              {user.email}
                            </Text>
                          </Stack>
                        </Td>
                        <Td>
                          {/* Badge de rol */}
                          <Badge colorScheme={roleColor[user.role]}>
                            {user.role}
                          </Badge>
                        </Td>
                        <Td>
                          {/* Badge de estado */}
                          <Badge colorScheme={statusColor[user.status]}>
                            {user.status}
                          </Badge>
                        </Td>
                        <Td>{user.lastLogin}</Td>
                        <Td textAlign="right">
                          {/* Acciones rapidas (solo visuales) */}
                          <HStack justify="flex-end">
                            <IconButton
                              aria-label="Editar"
                              icon={<FiEdit2 />}
                              size="sm"
                              variant="ghost"
                            />
                            <IconButton
                              aria-label="Reset"
                              icon={<FiLock />}
                              size="sm"
                              variant="ghost"
                            />
                            <IconButton
                              aria-label="Eliminar"
                              icon={<FiTrash2 />}
                              size="sm"
                              colorScheme="red"
                              variant="ghost"
                            />
                          </HStack>
                        </Td>
                      </Tr>
                    ))}
                  </Tbody>
                </Table>

                {/* Separador entre tabla y metricas */}
                <Divider my="6" />

                {/* Tarjetas de resumen rapido */}
                <SimpleGrid columns={{ base: 1, md: 3 }} spacing="4">
                  <Card variant="outline">
                    <CardBody>
                      <Text fontSize="sm" color="muted">
                        Usuarios activos
                      </Text>
                      <Heading size="md">18</Heading>
                    </CardBody>
                  </Card>
                  <Card variant="outline">
                    <CardBody>
                      <Text fontSize="sm" color="muted">
                        Invitaciones pendientes
                      </Text>
                      <Heading size="md">4</Heading>
                    </CardBody>
                  </Card>
                  <Card variant="outline">
                    <CardBody>
                      <Text fontSize="sm" color="muted">
                        Roles personalizados
                      </Text>
                      <Heading size="md">3</Heading>
                    </CardBody>
                  </Card>
                </SimpleGrid>
              </CardBody>
            </Card>
          </SimpleGrid>
        </Stack>
      </Container>
    </Box>
  );
}
