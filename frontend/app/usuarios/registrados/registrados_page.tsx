"use client";

// Navegacion y componentes UI usados por la vista.
import NextLink from "next/link";
import { useRef, useState } from "react";
import {
  AlertDialog,
  AlertDialogBody,
  AlertDialogContent,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogOverlay,
  Avatar,
  Badge,
  Box,
  Button,
  Card,
  CardBody,
  CardHeader,
  Container,
  Flex,
  HStack,
  Heading,
  SimpleGrid,
  Stack,
  Tag,
  TagLabel,
  Text,
  useDisclosure,
  useToast,
} from "@chakra-ui/react";
import { UserCardActions } from "#components/user-actions";
import { useUsersStore } from "#data/users-store";

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
  const { users: rows, setUsers: setRows } = useUsersStore();
  const [pendingAction, setPendingAction] = useState<{
    type: "delete" | "edit";
    userId: string;
  } | null>(null);
  const { isOpen, onOpen, onClose } = useDisclosure();
  const cancelRef = useRef<HTMLButtonElement>(null);
  const toast = useToast();

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
            <Heading size="lg">Usuarios registrados</Heading>
            <Text color="muted">
              Lista visual de usuarios con roles y permisos.
            </Text>
            <HStack spacing="4" flexWrap="wrap">
              <Button as={NextLink} href="/usuarios" variant="ghost">
                Usuarios
              </Button>
              <Button as={NextLink} href="/usuarios/registrados" colorScheme="purple">
                Usuarios registrados
              </Button>
              <Button as={NextLink} href="/usuarios/logs" variant="ghost">
                Logs de usuarios
              </Button>
            </HStack>
          </Stack>

          {/* Grid de tarjetas por usuario */}
          <SimpleGrid columns={{ base: 1, md: 2, xl: 3 }} spacing="6">
            {/* Renderizado de cada card con datos mock */}
            {rows.map((user) => (
              <Card key={user.id} variant="outline">
                <CardHeader>
                  <Flex
                    align="center"
                    justify="space-between"
                    gap="4"
                    wrap="wrap"
                  >
                    <HStack spacing="4" minW={0} flex="1">
                      <Avatar name={user.name} />
                      <Stack spacing="1" minW={0}>
                        <Heading size="sm" noOfLines={1}>
                          {user.name}
                        </Heading>
                        <Text fontSize="sm" color="muted" noOfLines={1}>
                          {user.email}
                        </Text>
                      </Stack>
                    </HStack>
                    <HStack spacing="2" flexShrink={0}>
                      {/* Badge con el rol del usuario */}
                      <Badge colorScheme={roleColor[user.role]}>
                        {user.role}
                      </Badge>
                      <UserCardActions
                        onEdit={() => openConfirm("edit", user.id)}
                        onDelete={() => openConfirm("delete", user.id)}
                      />
                    </HStack>
                  </Flex>
                </CardHeader>
                <CardBody>
                  <Stack spacing="3">
                    <Text fontSize="sm" color="muted">
                      Equipo: {user.team || "Sin equipo"}
                    </Text>
                    <Stack spacing="2">
                      <Text fontSize="sm" fontWeight="semibold">
                        Permisos
                      </Text>
                      {/* Lista visual de permisos */}
                      <HStack spacing="2" flexWrap="wrap">
                        {(user.permissions?.length ? user.permissions : ["sin_permisos"]).map(
                          (permission) => (
                            <Tag
                              key={permission}
                              size="sm"
                              variant="subtle"
                              colorScheme="purple"
                            >
                              <TagLabel>{permission}</TagLabel>
                            </Tag>
                          )
                        )}
                      </HStack>
                    </Stack>
                  </Stack>
                </CardBody>
              </Card>
            ))}
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
