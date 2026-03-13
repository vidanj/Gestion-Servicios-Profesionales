"use client";

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

const roleColor: Record<string, string> = {
  admin: "purple",
  cliente: "blue",
  profesionista: "green",
};

const toaster = createToaster({ placement: "top-end" });

export default function RegisteredUsersPage() {
  const { users: rows, setUsers: setRows, loading, error, refresh } = useUsersStore();
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
    if (!pendingAction) return;

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
    } catch (err) {
      toaster.create({
        title: "Ocurrio un error",
        description: "Intenta nuevamente o revisa la consola.",
        type: "error",
        duration: 4000,
      });
      console.error("Error al procesar la accion de usuario:", err);
    } finally {
      handleClose();
    }
  };

  const dialogCopy =
    pendingAction?.type === "delete"
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
          <Stack gap="2">
            <Heading size="lg">Usuarios registrados</Heading>
            <Text color="muted">Lista visual de usuarios con roles y permisos.</Text>
            <HStack gap="4" flexWrap="wrap">
              <Button asChild variant="ghost">
                <NextLink href="/usuarios">Usuarios</NextLink>
              </Button>
              <Button asChild colorPalette="purple">
                <NextLink href="/usuarios/registrados">Usuarios registrados</NextLink>
              </Button>
              <Button asChild variant="ghost">
                <NextLink href="/usuarios/logs">Logs de usuarios</NextLink>
              </Button>
              <Button asChild variant="ghost"><NextLink href="/usuarios/grafica">Gráfica</NextLink></Button>
            </HStack>
          </Stack>

          {/* Error de API */}
          {error && (
            <Text data-testid="registrados-error" color="red.500">{error}</Text>
          )}

          {/* Loading */}
          {loading && (
            <Text data-testid="registrados-loading" textAlign="center" py="4">
              Cargando...
            </Text>
          )}

          {/* Grid de tarjetas */}
          {!loading && (
            <SimpleGrid columns={{ base: 1, md: 2, lg: 3 }} gap="4" w="100%">
              {rows.map((user: User) => (
                <Card.Root key={user.id} variant="outline" data-testid="registrado-card" style={{ background: "#272727", color: "#fff" }}>
                  <CardHeader>
                    <Flex align="center" justify="space-between" gap="4" wrap="wrap" py="2" px="2">
                      <HStack gap="4" minW={0} flex="1">
                        <AvatarRoot>
                          <AvatarFallback name={user.name} />
                        </AvatarRoot>
                        <Stack gap="1" minW={0}>
                          <Heading size="sm" lineClamp={1}>{user.name}</Heading>
                          <Text fontSize="sm" color="muted" lineClamp={1}>{user.email}</Text>
                        </Stack>
                      </HStack>
                      <HStack gap="2" flexShrink={0}>
                        <Badge colorPalette={roleColor[user.role] ?? "gray"}>
                          {user.role}
                        </Badge>
                        <UserCardActions
                          onEdit={() => openConfirm("edit", user.id)}
                          onDelete={() => openConfirm("delete", user.id)}
                        />
                      </HStack>
                    </Flex>
                  </CardHeader>
                </Card.Root>
              ))}

              {/* Sin resultados */}
              {rows.length === 0 && (
                <Text
                  data-testid="registrados-empty"
                  color="muted"
                  textAlign="center"
                  py="4"
                >
                  Sin usuarios registrados.
                </Text>
              )}
            </SimpleGrid>
          )}
        </Stack>

        {/* Modal confirmación */}
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
      </Container>
    </Box>
  );
}