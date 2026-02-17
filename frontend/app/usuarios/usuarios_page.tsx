"use client";

// Navegacion y componentes UI de Next/Chakra usados en la vista.
import NextLink from "next/link";
import { useRef, useState } from "react";
import {
  AlertDialog,
  AlertDialogBody,
  AlertDialogContent,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogOverlay,
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
  useDisclosure,
  useToast,
} from "@chakra-ui/react";
import { FiEdit2, FiLock, FiPlus, FiTrash2 } from "react-icons/fi";
import { useUsersStore } from "#data/users-store";

// Colores por rol para los badges en la tabla.
const roleColor: Record<string, string> = {
  admin: "purple",
  manager: "blue",
  usuario: "gray",
  soporte: "green",
  auditor: "orange",
};

// Colores por estado para los badges de status.
const statusColor: Record<string, string> = {
  activo: "green",
  suspendido: "red",
  pendiente: "yellow",
};

const buildUserId = (rows: { id: string }[]) => {
  const maxId = rows.reduce((currentMax, user) => {
    const match = user.id.match(/USR-(\d+)/);
    const value = match ? Number(match[1]) : 0;
    return Math.max(currentMax, value);
  }, 1000);

  return `USR-${String(maxId + 1).padStart(4, "0")}`;
};

const formatDateTime = (value: Date) =>
  value.toISOString().slice(0, 16).replace("T", " ");

const defaultForm = {
  name: "",
  email: "",
  username: "",
  role: "",
  isActive: true,
};

// Pagina de ejemplo para usuarios.
export default function UsersCrudPage() {
  const { users: rows, setUsers: setRows } = useUsersStore();
  const [formValues, setFormValues] = useState(defaultForm);
  const [pendingAction, setPendingAction] = useState<{
    type: "delete" | "edit";
    userId: string;
  } | null>(null);
  const { isOpen, onOpen, onClose } = useDisclosure();
  const cancelRef = useRef<HTMLButtonElement>(null);
  const toast = useToast();

  const handleFormChange = (
    key: "name" | "email" | "username" | "role",
    value: string
  ) => {
    setFormValues((prev) => ({ ...prev, [key]: value }));
  };

  const handleCreateUser = () => {
    if (!formValues.name.trim() || !formValues.email.trim() || !formValues.username.trim()) {
      toast({
        title: "Campos obligatorios",
        description: "Completa nombre, correo y usuario antes de continuar.",
        status: "warning",
        duration: 4000,
        isClosable: true,
      });
      return;
    }

    if (!formValues.role) {
      toast({
        title: "Rol requerido",
        description: "Selecciona un rol para crear el usuario.",
        status: "warning",
        duration: 4000,
        isClosable: true,
      });
      return;
    }

    try {
      const nextUser = {
        id: buildUserId(rows),
        name: formValues.name.trim(),
        email: formValues.email.trim(),
        username: formValues.username.trim(),
        role: formValues.role,
        status: formValues.isActive ? "activo" : "suspendido",
        lastLogin: formatDateTime(new Date()),
        team: "Sin equipo",
        permissions: ["perfil:ver"],
      };

      setRows((prev) => [...prev, nextUser]);
      setFormValues(defaultForm);
      toast({
        title: "Usuario creado",
        description: "El usuario fue agregado correctamente.",
        status: "success",
        duration: 3000,
        isClosable: true,
      });
    } catch (error) {
      toast({
        title: "No se pudo crear",
        description: "Intenta nuevamente o revisa la consola.",
        status: "error",
        duration: 4000,
        isClosable: true,
      });
      console.error("Error al crear usuario:", error);
    }
  };

  const openConfirm = (type: "delete" | "edit", userId: string) => {
    setPendingAction({ type, userId });
    onOpen();
  };

  const handleClose = () => {
    setPendingAction(null);
    onClose();
  };

  const handleConfirm = () => {
    if (!pendingAction) {
      return;
    }

    const target = rows.find((user) => user.id === pendingAction.userId);
    if (!target) {
      toast({
        title: "Usuario no encontrado",
        description: "No se pudo completar la accion solicitada.",
        status: "error",
        duration: 4000,
        isClosable: true,
      });
      handleClose();
      return;
    }

    try {
      if (pendingAction.type === "delete") {
        setRows((prev) => prev.filter((user) => user.id !== target.id));
        toast({
          title: "Usuario eliminado",
          description: `${target.name} fue eliminado correctamente.`,
          status: "success",
          duration: 3000,
          isClosable: true,
        });
      } else {
        toast({
          title: "Edicion confirmada",
          description: `${target.name} esta listo para editarse.`,
          status: "success",
          duration: 3000,
          isClosable: true,
        });
      }
    } catch (error) {
      toast({
        title: "Ocurrio un error",
        description: "Intenta nuevamente o revisa la consola.",
        status: "error",
        duration: 4000,
        isClosable: true,
      });
      console.error("Error al procesar la accion de usuario:", error);
    } finally {
      handleClose();
    }
  };

  const dialogCopy = pendingAction?.type === "delete"
    ? {
        title: "Confirmar eliminacion",
        body: "Esta accion eliminara el usuario seleccionado. Deseas continuar?",
        confirmLabel: "Eliminar",
        confirmScheme: "red",
      }
    : {
        title: "Confirmar edicion",
        body: "Deseas continuar con la edicion de este usuario?",
        confirmLabel: "Confirmar",
        confirmScheme: "purple",
      };

  return (
    <Box py={{ base: 10, md: 16 }}>
      <Container maxW="container.xl">
        <Stack spacing="6">
          {/* Encabezado de la pagina y navegacion entre vistas */}
          <Stack spacing="2">
            <Heading size="lg">Usuarios</Heading>
            <Text color="muted">
              Crea, edita y administra usuarios. Vista solamente visual.
            </Text>
            <HStack spacing="4" flexWrap="wrap">
              <Button as={NextLink} href="/usuarios" colorScheme="purple">
                Usuarios
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
                    <Input
                      placeholder="Ej. Andrea Torres"
                      value={formValues.name}
                      onChange={(event) => handleFormChange("name", event.target.value)}
                    />
                  </FormControl>
                  {/* Campo: correo */}
                  <FormControl>
                    <FormLabel>Correo</FormLabel>
                    <Input
                      type="email"
                      placeholder="correo@empresa.com"
                      value={formValues.email}
                      onChange={(event) => handleFormChange("email", event.target.value)}
                    />
                  </FormControl>
                  {/* Campo: username */}
                  <FormControl>
                    <FormLabel>Usuario</FormLabel>
                    <Input
                      placeholder="usuario"
                      value={formValues.username}
                      onChange={(event) => handleFormChange("username", event.target.value)}
                    />
                  </FormControl>
                  {/* Campo: rol */}
                  <FormControl>
                    <FormLabel>Rol</FormLabel>
                    <Select
                      placeholder="Selecciona un rol"
                      value={formValues.role}
                      onChange={(event) => handleFormChange("role", event.target.value)}
                    >
                      <option>admin</option>
                      <option>manager</option>
                      <option>usuario</option>
                      <option>soporte</option>
                      <option>auditor</option>
                    </Select>
                  </FormControl>
                  {/* Toggle: estado activo */}
                  <HStack justify="space-between">
                    <FormLabel mb="0">Activo</FormLabel>
                    <Switch
                      isChecked={formValues.isActive}
                      onChange={(event) =>
                        setFormValues((prev) => ({
                          ...prev,
                          isActive: event.target.checked,
                        }))
                      }
                    />
                  </HStack>
                  {/* Acciones principales del formulario */}
                  <ButtonGroup>
                    <Button
                      leftIcon={<FiPlus />}
                      colorScheme="purple"
                      onClick={handleCreateUser}
                    >
                      Crear usuario
                    </Button>
                    <Button
                      variant="outline"
                      onClick={() => setFormValues(defaultForm)}
                    >
                      Limpiar
                    </Button>
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
                    {rows.map((user) => (
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
                          <Badge colorScheme={roleColor[user.role] || "gray"}>
                            {user.role}
                          </Badge>
                        </Td>
                        <Td>
                          {/* Badge de estado */}
                          <Badge colorScheme={statusColor[user.status || "pendiente"]}>
                            {user.status || "pendiente"}
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
                              onClick={() => openConfirm("edit", user.id)}
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
                              onClick={() => openConfirm("delete", user.id)}
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

          <AlertDialog
            isOpen={isOpen}
            leastDestructiveRef={cancelRef}
            onClose={handleClose}
          >
            <AlertDialogOverlay>
              <AlertDialogContent>
                <AlertDialogHeader fontSize="lg" fontWeight="bold">
                  {dialogCopy.title}
                </AlertDialogHeader>

                <AlertDialogBody>{dialogCopy.body}</AlertDialogBody>

                <AlertDialogFooter>
                  <Button ref={cancelRef} onClick={handleClose} variant="ghost">
                    Cancelar
                  </Button>
                  <Button
                    colorScheme={dialogCopy.confirmScheme}
                    onClick={handleConfirm}
                    ml={3}
                  >
                    {dialogCopy.confirmLabel}
                  </Button>
                </AlertDialogFooter>
              </AlertDialogContent>
            </AlertDialogOverlay>
          </AlertDialog>
        </Stack>
      </Container>
    </Box>
  );
}
