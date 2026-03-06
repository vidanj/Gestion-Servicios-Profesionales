"use client";

// Navegacion y componentes UI usados por la vista.
import NextLink from "next/link";
import { useRef, useState } from "react";
import {
  AvatarFallback,
  AvatarRoot,
  Badge,
  Box,
  Button,
  Card,
  CardBody,
  CardHeader,
  Container,
  DialogBackdrop,
  DialogBody,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogPositioner,
  DialogRoot,
  Flex,
  HStack,
  Heading,
  SimpleGrid,
  Stack,
  TagLabel,
  TagRoot,
  Text,
  ToastCloseTrigger,
  ToastDescription,
  ToastRoot,
  ToastTitle,
  Toaster,
  createToaster,
  useDisclosure,
} from "@chakra-ui/react";
import { UserCardActions } from "../../../src/components/user-actions";
import { useUsersStore, type User } from "../../../src/data/users-store";

// Colores por rol para diferenciar visualmente.
const roleColor: Record<string, string> = {
  admin: "purple",
  manager: "blue",
  usuario: "gray",
  soporte: "green",
  auditor: "orange",
};

const toaster = createToaster({ placement: "top-end" });

// Pagina de listado de usuarios con permisos.
export default function RegisteredUsersPage() {
  const { users: rows, setUsers: setRows } = useUsersStore();
  const [pendingAction, setPendingAction] = useState<{
    type: "delete" | "edit";
    userId: string;
  } | null>(null);
  const { open: isOpen, onOpen, onClose } = useDisclosure();
  const cancelRef = useRef<HTMLButtonElement>(null);
  

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

    const target = rows.find((user: User) => user.id === pendingAction.userId);
    if (!target) {
      toaster.create({
        title: "Usuario no encontrado",
        description: "No se pudo completar la accion solicitada.",
        type: "error",
        duration: 4000,

      });
      handleClose();
      return;
    }

    try {
      if (pendingAction.type === "delete") {
        setRows((prev: User[]) => prev.filter((user: User) => user.id !== target.id));
        toaster.create({
          title: "Usuario eliminado",
          description: `${target.name} fue eliminado correctamente.`,
          type: "success",
          duration: 3000,

        });
      } else {
        toaster.create({
          title: "Edicion confirmada",
          description: `${target.name} esta listo para editarse.`,
          type: "success",
          duration: 3000,

        });
      }
    } catch (error) {
      toaster.create({
        title: "Ocurrio un error",
        description: "Intenta nuevamente o revisa la consola.",
        type: "error",
        duration: 4000,

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
        confirmPalette: "red",
      }
    : {
        title: "Confirmar edicion",
        body: "Deseas continuar con la edicion de este usuario?",
        confirmLabel: "Confirmar",
        confirmPalette: "purple",
      };

  return (
    <Box py={{ base: 10, md: 16 }}>
      <Toaster toaster={toaster}>
        {(toast) => (
          <ToastRoot key={toast.id}>
            <ToastTitle>{String(toast.title || "")}</ToastTitle>
            {toast.description && (
              <ToastDescription>{String(toast.description)}</ToastDescription>
            )}
            <ToastCloseTrigger />
          </ToastRoot>
        )}
      </Toaster>
      <Container maxW="container.xl">
        <Stack gap="6">
          {/* Encabezado de la pagina y navegacion entre vistas */}
          <Stack gap="2">
            <Heading size="lg">Usuarios registrados</Heading>
            <Text color="muted">
              Lista visual de usuarios con roles y permisos.
            </Text>
            <HStack gap="4" flexWrap="wrap">
              <Button asChild variant="ghost"><NextLink href="/usuarios">Usuarios</NextLink></Button>
              <Button asChild colorPalette="purple"><NextLink href="/usuarios/registrados">Usuarios registrados</NextLink></Button>
              <Button asChild variant="ghost"><NextLink href="/usuarios/logs">Logs de usuarios</NextLink></Button>
            </HStack>
          </Stack>

          {/* Grid de tarjetas por usuario */}
          <SimpleGrid columns={{ base: 1, md: 2, xl: 3 }} gap="6">
            {/* Renderizado de cada card con datos mock */}
            {rows.map((user: User) => (
              <Card.Root key={user.id} variant="outline">
                <CardHeader>
                  <Flex
                    align="center"
                    justify="space-between"
                    gap="4"
                    wrap="wrap"
                  >
                    <HStack gap="4" minW={0} flex="1">
                      <AvatarRoot><AvatarFallback name={user.name} /></AvatarRoot>
                      <Stack gap="1" minW={0}>
                        <Heading size="sm" lineClamp={1}>
                          {user.name}
                        </Heading>
                        <Text fontSize="sm" color="muted" lineClamp={1}>
                          {user.email}
                        </Text>
                      </Stack>
                    </HStack>
                    <HStack gap="2" flexShrink={0}>
                      {/* Badge con el rol del usuario */}
                      <Badge colorPalette={roleColor[user.role]}>
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
                  <Stack gap="3">
                    <Text fontSize="sm" color="muted">
                      Equipo: {user.team || "Sin equipo"}
                    </Text>
                    <Stack gap="2">
                      <Text fontSize="sm" fontWeight="semibold">
                        Permisos
                      </Text>
                      {/* Lista visual de permisos */}
                      <HStack gap="2" flexWrap="wrap">
                        {(user.permissions?.length ? user.permissions : ["sin_permisos"]).map(
                          (permission: string) => (
                            <TagRoot
                              key={permission}
                              size="sm"
                              variant="subtle"
                              colorPalette="purple"
                            >
                              <TagLabel>{permission}</TagLabel>
                            </TagRoot>
                          )
                        )}
                      </HStack>
                    </Stack>
                  </Stack>
                </CardBody>
              </Card.Root>
            ))}
          </SimpleGrid>

          <DialogRoot
            open={isOpen}
            onOpenChange={({ open }) => !open && handleClose()}
            
          >
            <DialogBackdrop />
              <DialogPositioner>
              <DialogContent>
                <DialogHeader fontSize="lg" fontWeight="bold">
                  {dialogCopy.title}
                </DialogHeader>

                <DialogBody>{dialogCopy.body}</DialogBody>

                <DialogFooter>
                  <Button ref={cancelRef} onClick={handleClose} variant="ghost">
                    Cancelar
                  </Button>
                  <Button
                    colorPalette={dialogCopy.confirmPalette}
                    onClick={handleConfirm}
                    ml={3}
                  >
                    {dialogCopy.confirmLabel}
                  </Button>
                </DialogFooter>
              </DialogContent>
            </DialogPositioner>
          </DialogRoot>
        </Stack>
      </Container>
    </Box>
  );
}
