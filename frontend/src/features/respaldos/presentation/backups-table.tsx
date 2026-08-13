import { Button, Card, HStack, Table, Text } from "@chakra-ui/react";
import { FiDownload } from "react-icons/fi";

import { Backup, formatBackupDate, formatFileSize } from "../domain/backup.model";

interface BackupsTableProps {
  backups: Backup[];
  isLoading: boolean;
  downloadingFile: string | null;
  onDownload: (fileName: string) => void;
}

export function BackupsTable({
  backups,
  isLoading,
  downloadingFile,
  onDownload,
}: BackupsTableProps) {
  return (
    <Card.Root>
      <Card.Header>
        <HStack justify="space-between">
          <Text fontWeight="semibold">Respaldos disponibles</Text>
          <Text fontSize="sm" color="fg.muted" data-testid="backups-count">
            {backups.length} {backups.length === 1 ? "archivo" : "archivos"}
          </Text>
        </HStack>
      </Card.Header>
      <Card.Body>
        {isLoading && <Text data-testid="backups-loading">Cargando respaldos…</Text>}

        {!isLoading && backups.length === 0 && (
          <Text color="fg.muted" data-testid="backups-empty">
            Todavía no hay respaldos generados. Usa «Generar respaldo» para crear el primero.
          </Text>
        )}

        {!isLoading && backups.length > 0 && (
          <Table.Root variant="outline" size="sm">
            <Table.Header>
              <Table.Row>
                <Table.ColumnHeader>Archivo</Table.ColumnHeader>
                <Table.ColumnHeader>Fecha de creación</Table.ColumnHeader>
                <Table.ColumnHeader>Tamaño</Table.ColumnHeader>
                <Table.ColumnHeader textAlign="right">Acciones</Table.ColumnHeader>
              </Table.Row>
            </Table.Header>
            <Table.Body>
              {backups.map((backup) => (
                <Table.Row key={backup.fileName} data-testid="backup-row">
                  <Table.Cell>
                    <Text fontWeight="semibold" data-testid="backup-filename">
                      {backup.fileName}
                    </Text>
                  </Table.Cell>
                  <Table.Cell>{formatBackupDate(backup.createdAt)}</Table.Cell>
                  <Table.Cell data-testid="backup-size">
                    {formatFileSize(backup.fileSizeBytes)}
                  </Table.Cell>
                  <Table.Cell textAlign="right">
                    <Button
                      size="sm"
                      variant="outline"
                      data-testid="backup-download-button"
                      loading={downloadingFile === backup.fileName}
                      onClick={() => onDownload(backup.fileName)}
                    >
                      <FiDownload />
                      Descargar
                    </Button>
                  </Table.Cell>
                </Table.Row>
              ))}
            </Table.Body>
          </Table.Root>
        )}
      </Card.Body>
    </Card.Root>
  );
}
