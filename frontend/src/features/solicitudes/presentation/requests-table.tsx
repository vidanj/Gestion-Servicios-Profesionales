import {
  Badge,
  Button,
  Card,
  HStack,
  IconButton,
  NativeSelect,
  Stack,
  Table,
  Text,
} from "@chakra-ui/react";
import { FiEdit2, FiTrash2, FiUsers } from "react-icons/fi";

import {
  levelLabel,
  RequestStatus,
  ServiceRequest,
  statusLabel,
  workModeLabel,
} from "../domain/request.model";

interface RequestsTableProps {
  requests: ServiceRequest[];
  onEdit: (id: string) => void;
  onDelete: (id: string) => void;
  onChangeStatus: (id: string, status: RequestStatus) => void;
}

const statusColor: Record<RequestStatus, string> = {
  publicada: "green",
  en_revision: "blue",
  entrevista: "purple",
  cerrada: "gray",
};

const levelColor = {
  junior: "gray",
  semi_senior: "orange",
  senior: "purple",
};

export function RequestsTable({ requests, onEdit, onDelete, onChangeStatus }: RequestsTableProps) {
  return (
    <Card.Root>
      <Card.Header>
        <HStack justify="space-between">
          <Text fontWeight="semibold">Vacantes publicadas</Text>
          <Button size="sm" variant="outline">
            Exportar
          </Button>
        </HStack>
      </Card.Header>
      <Card.Body>
        {/* Bloque: tabla principal con seguimiento de vacantes y postulantes */}
        <Table.Root variant="outline" size="sm">
          <Table.Header>
            <Table.Row>
              <Table.ColumnHeader>Vacante</Table.ColumnHeader>
              <Table.ColumnHeader>Servicio base</Table.ColumnHeader>
              <Table.ColumnHeader>Empleador</Table.ColumnHeader>
              <Table.ColumnHeader>Modalidad / Nivel</Table.ColumnHeader>
              <Table.ColumnHeader>Postulantes</Table.ColumnHeader>
              <Table.ColumnHeader>Estado</Table.ColumnHeader>
              <Table.ColumnHeader>Pago</Table.ColumnHeader>
              <Table.ColumnHeader textAlign="right">Acciones</Table.ColumnHeader>
            </Table.Row>
          </Table.Header>
          <Table.Body>
            {requests.map((request) => (
              <Table.Row key={request.id}>
                <Table.Cell>
                  <Stack gap="0">
                    <Text fontWeight="semibold">{request.code}</Text>
                    <Text fontSize="sm" color="muted">
                      {request.jobTitle}
                    </Text>
                  </Stack>
                </Table.Cell>
                <Table.Cell>{request.serviceName}</Table.Cell>
                <Table.Cell>{request.employerName}</Table.Cell>
                <Table.Cell>
                  <Stack gap="2">
                    <Text fontSize="sm">{workModeLabel[request.workMode]}</Text>
                    <Badge colorScheme={levelColor[request.level]}>
                      {levelLabel[request.level]}
                    </Badge>
                  </Stack>
                </Table.Cell>
                <Table.Cell>{request.applicantsCount}</Table.Cell>
                <Table.Cell>
                  <Stack gap="2">
                    <Badge colorScheme={statusColor[request.status]}>
                      {statusLabel[request.status]}
                    </Badge>
                    <NativeSelect.Root size="xs">
                      <NativeSelect.Field
                        value={request.status}
                        onChange={(event) => onChangeStatus(request.id, event.target.value as RequestStatus)}
                      >
                        <option value="publicada">Publicada</option>
                        <option value="en_revision">En revisión</option>
                        <option value="entrevista">En entrevista</option>
                        <option value="cerrada">Cerrada</option>
                      </NativeSelect.Field>
                    </NativeSelect.Root>
                  </Stack>
                </Table.Cell>
                <Table.Cell>L. {request.salary.toLocaleString("es-HN")}</Table.Cell>
                <Table.Cell textAlign="right">
                  <HStack justify="flex-end">
                    <IconButton aria-label="Ver perfiles" size="sm" variant="ghost">
                      <FiUsers />
                    </IconButton>
                    <IconButton aria-label="Editar vacante" size="sm" variant="ghost" onClick={() => onEdit(request.id)}>
                      <FiEdit2 />
                    </IconButton>
                    <IconButton aria-label="Eliminar vacante" size="sm" colorScheme="red" variant="ghost" onClick={() => onDelete(request.id)}>
                      <FiTrash2 />
                    </IconButton>
                  </HStack>
                </Table.Cell>
              </Table.Row>
            ))}
          </Table.Body>
        </Table.Root>
      </Card.Body>
    </Card.Root>
  );
}
