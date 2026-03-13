"use client";

import { useEffect, useState } from "react";
import NextLink from "next/link";
import {
  Box,
  Button,
  Card,
  Container,
  Field,
  HStack,
  Heading,
  Input,
  NativeSelect,
  SimpleGrid,
  Stack,
  Text,
} from "@chakra-ui/react";

import { useRequestsController } from "@/features/solicitudes/application/use-requests-controller";
import { RequestsForm } from "@/features/solicitudes/presentation/requests-form";
import { RequestsTable } from "@/features/solicitudes/presentation/requests-table";

export default function RequestsPage() {
  const [mounted, setMounted] = useState(false);
  const controller = useRequestsController();

  useEffect(() => setMounted(true), []);
  if (!mounted) return null;

  return (
    <Box py={{ base: 10, md: 16 }}>
      <Container maxW="container.xl">
        <Stack gap="6">
          {/* Bloque: encabezado y navegación rápida entre vistas del módulo */}
          <Stack gap="2">
            <Heading size="lg">Gestión de vacantes y postulaciones</Heading>
            <Text color="muted">
              Publica trabajos freelance y da seguimiento a postulantes desde una sola pantalla.
            </Text>
            {controller.errorMessage ? (
              <Text color="red.500" fontSize="sm">
                {controller.errorMessage}
              </Text>
            ) : null}
            <HStack gap="4" flexWrap="wrap">
              <Button asChild colorPalette="purple">
                <NextLink href="/solicitudes">Solicitudes</NextLink>
              </Button>
              <Button asChild variant="ghost">
                <NextLink href="/usuarios">Usuarios</NextLink>
              </Button>
              <Button asChild variant="ghost">
                <NextLink href="/usuarios/logs">Logs</NextLink>
              </Button>
            </HStack>
          </Stack>

          {/* Bloque: métricas simples para lectura rápida */}
          <SimpleGrid columns={{ base: 1, md: 4 }} gap="4">
            <Card.Root variant="outline">
              <Card.Body>
                <Text fontSize="sm" color="muted">
                  Total
                </Text>
                <Heading size="md">{controller.summary.total}</Heading>
              </Card.Body>
            </Card.Root>
            <Card.Root variant="outline">
              <Card.Body>
                <Text fontSize="sm" color="muted">
                  Publicadas
                </Text>
                <Heading size="md">{controller.summary.published}</Heading>
              </Card.Body>
            </Card.Root>
            <Card.Root variant="outline">
              <Card.Body>
                <Text fontSize="sm" color="muted">
                  En revisión
                </Text>
                <Heading size="md">{controller.summary.reviewing}</Heading>
              </Card.Body>
            </Card.Root>
            <Card.Root variant="outline">
              <Card.Body>
                <Text fontSize="sm" color="muted">
                  Entrevistas
                </Text>
                <Heading size="md">{controller.summary.interviewing}</Heading>
              </Card.Body>
            </Card.Root>
          </SimpleGrid>

          {/* Bloque: filtros de búsqueda y estado */}
          <Card.Root variant="outline">
            <Card.Body>
              <SimpleGrid columns={{ base: 1, md: 2 }} gap="4">
                <Field.Root>
                  <Field.Label>Búsqueda</Field.Label>
                  <Input
                    placeholder="Buscar por código, empresa, vacante o habilidad"
                    value={controller.searchText}
                    onChange={(event) => controller.setSearchText(event.target.value)}
                  />
                  {controller.isSearchDebouncing ? (
                    <Text fontSize="xs" color="muted" mt="1">
                      Esperando 400 ms para aplicar búsqueda...
                    </Text>
                  ) : null}
                </Field.Root>
                <Field.Root>
                  <Field.Label>Estado</Field.Label>
                  <NativeSelect.Root>
                    <NativeSelect.Field
                      value={controller.statusFilter}
                      onChange={(event) =>
                        controller.setStatusFilter(
                          event.target.value as "todos" | "publicada" | "en_revision" | "entrevista" | "cerrada",
                        )
                      }
                    >
                      <option value="todos">Todos</option>
                      <option value="publicada">Publicada</option>
                      <option value="en_revision">En revisión</option>
                      <option value="entrevista">En entrevista</option>
                      <option value="cerrada">Cerrada</option>
                    </NativeSelect.Field>
                  </NativeSelect.Root>
                </Field.Root>
              </SimpleGrid>
            </Card.Body>
          </Card.Root>

          {/* Bloque: panel principal dividido en formulario y tabla */}
          <SimpleGrid columns={{ base: 1, xl: 3 }} gap="6">
            <RequestsForm
              draft={controller.draft}
              editingId={controller.editingId}
              onSelectService={controller.selectService}
              onChangeField={controller.setDraftField}
              onSave={controller.saveRequest}
              onReset={controller.resetDraft}
            />

            <Box gridColumn={{ xl: "span 2" }}>
              <RequestsTable
                requests={controller.requests}
                onEdit={controller.editRequest}
                onDelete={controller.removeRequest}
                onChangeStatus={controller.updateStatus}
              />
            </Box>
          </SimpleGrid>
        </Stack>
      </Container>
    </Box>
  );
}
