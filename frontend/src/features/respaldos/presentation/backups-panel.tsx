"use client";

import NextLink from "next/link";
import { Box, Button, Container, HStack, Heading, Stack, Text } from "@chakra-ui/react";
import { FiDatabase } from "react-icons/fi";

import { useBackupsController } from "../application/use-backups-controller";
import { BackupsTable } from "./backups-table";

// Navegacion propia de esta pantalla. Las cuatro secciones existentes del panel
// escriben sus enlaces a mano en cada pagina; aqui solo se enlaza hacia ellas, sin
// modificarlas. La integracion inversa queda para el responsable de ese modulo.
const PANEL_LINKS = [
  { href: "/usuarios", label: "Usuarios" },
  { href: "/usuarios/registrados", label: "Registrados" },
  { href: "/usuarios/logs", label: "Bitácora" },
  { href: "/usuarios/grafica", label: "Gráfica" },
];

export function BackupsPanel() {
  const {
    backups,
    isLoading,
    isGenerating,
    downloadingFile,
    errorMessage,
    generateBackup,
    downloadBackup,
  } = useBackupsController();

  return (
    <Container maxW="6xl" py="8">
      <Stack gap="6">
        <HStack gap="4" wrap="wrap">
          {PANEL_LINKS.map((link) => (
            <NextLink key={link.href} href={link.href} style={{ textDecoration: "none" }}>
              <Text fontSize="sm" color="fg.muted">
                {link.label}
              </Text>
            </NextLink>
          ))}
          <Text fontSize="sm" fontWeight="semibold" data-testid="backups-nav-current">
            Respaldos
          </Text>
        </HStack>

        <HStack justify="space-between" wrap="wrap" gap="4">
          <Stack gap="1">
            <Heading size="lg" data-testid="backups-title">
              Respaldos de base de datos
            </Heading>
            <Text fontSize="sm" color="fg.muted">
              Genera un volcado completo de la base de datos y descárgalo.
            </Text>
          </Stack>

          <Button
            colorPalette="blue"
            data-testid="generate-backup-button"
            loading={isGenerating}
            loadingText="Generando…"
            onClick={() => void generateBackup()}
          >
            <FiDatabase />
            Generar respaldo
          </Button>
        </HStack>

        {/* El respaldo vive en el sistema de archivos del contenedor hasta que se
            resuelva el issue #126. Sin volumen montado desaparece al desplegar, y
            sin este aviso eso se lee como un fallo de la aplicacion. */}
        <Box
          borderWidth="1px"
          borderRadius="md"
          borderColor="orange.300"
          bg="orange.50"
          p="3"
          data-testid="backups-persistence-warning"
        >
          <Text fontSize="sm">
            Los respaldos se guardan en el servidor de la aplicación. Si el servicio se
            redespliega sin un volumen persistente, esta lista puede aparecer vacía.
            Descarga los respaldos que quieras conservar.
          </Text>
        </Box>

        {errorMessage && (
          <Box
            borderWidth="1px"
            borderRadius="md"
            borderColor="red.300"
            bg="red.50"
            p="3"
            data-testid="backups-error"
          >
            <Text fontSize="sm" color="red.700">
              {errorMessage}
            </Text>
          </Box>
        )}

        <BackupsTable
          backups={backups}
          isLoading={isLoading}
          downloadingFile={downloadingFile}
          onDownload={(fileName) => void downloadBackup(fileName)}
        />
      </Stack>
    </Container>
  );
}
