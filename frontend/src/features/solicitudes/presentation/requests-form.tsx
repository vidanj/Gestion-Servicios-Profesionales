import { Button, ButtonGroup, Card, Field, Input, NativeSelect, Stack, Text, Textarea } from "@chakra-ui/react";

import { availableServices, RequestDraft, RequestStatus } from "../domain/request.model";

interface RequestsFormProps {
  draft: RequestDraft;
  editingId: string | null;
  onSelectService: (serviceId: string) => void;
  onChangeField: (field: keyof RequestDraft, value: string) => void;
  onSave: () => void;
  onReset: () => void;
}

export function RequestsForm({ draft, editingId, onSelectService, onChangeField, onSave, onReset }: RequestsFormProps) {
  const selectedService = availableServices.find((service) => service.id === draft.serviceId)
  const dependentCategory = selectedService?.category || draft.category

  return (
    <Card.Root>
      <Card.Header>{editingId ? "Editar vacante" : "Publicar vacante"}</Card.Header>
      <Card.Body>
        <Stack gap="4">
          {/* Bloque: crear solicitud desde un servicio base */}
          <Field.Root>
            <Field.Label>Servicio seleccionado</Field.Label>
            <NativeSelect.Root>
              <NativeSelect.Field
                value={draft.serviceId}
                onChange={(event) => onSelectService(event.target.value)}
              >
                {availableServices.map((service) => (
                  <option key={service.id} value={service.id}>
                    {service.name}
                  </option>
                ))}
              </NativeSelect.Field>
            </NativeSelect.Root>
            <Text fontSize="xs" color="muted">
              Este combo actualiza la categoría sugerida en tiempo real.
            </Text>
          </Field.Root>

          {/* Bloque: datos principales de la vacante */}
          <Field.Root>
            <Field.Label>Empresa / Empleador</Field.Label>
            <Input
              value={draft.employerName}
              onChange={(event) => onChangeField("employerName", event.target.value)}
              placeholder="Ej. Comercial Rivera"
            />
          </Field.Root>

          <Field.Root>
            <Field.Label>Título de la vacante</Field.Label>
            <Input
              value={draft.jobTitle}
              onChange={(event) => onChangeField("jobTitle", event.target.value)}
              placeholder="Ej. Frontend Developer React"
            />
          </Field.Root>

          <Field.Root>
            <Field.Label>Categoría dependiente</Field.Label>
            <Input value={dependentCategory} readOnly />
            <Text fontSize="xs" color="muted">
              Se actualiza automáticamente según el servicio elegido en tiempo real.
            </Text>
          </Field.Root>

          {/* Bloque: modalidad, nivel y estado de seguimiento */}
          <Field.Root>
            <Field.Label>Modalidad</Field.Label>
            <NativeSelect.Root>
              <NativeSelect.Field
                value={draft.workMode}
                onChange={(event) => onChangeField("workMode", event.target.value)}
              >
                <option value="remoto">Remoto</option>
                <option value="hibrido">Híbrido</option>
                <option value="presencial">Presencial</option>
              </NativeSelect.Field>
            </NativeSelect.Root>
          </Field.Root>

          <Field.Root>
            <Field.Label>Nivel de experiencia</Field.Label>
            <NativeSelect.Root>
              <NativeSelect.Field
                value={draft.level}
                onChange={(event) => onChangeField("level", event.target.value)}
              >
                <option value="junior">Junior</option>
                <option value="semi_senior">Semi Senior</option>
                <option value="senior">Senior</option>
              </NativeSelect.Field>
            </NativeSelect.Root>
          </Field.Root>

          <Field.Root>
            <Field.Label>Estado de publicación</Field.Label>
            <NativeSelect.Root>
              <NativeSelect.Field
                value={draft.status}
                onChange={(event) => onChangeField("status", event.target.value as RequestStatus)}
              >
                <option value="publicada">Publicada</option>
                <option value="en_revision">En revisión</option>
                <option value="entrevista">En entrevista</option>
                <option value="cerrada">Cerrada</option>
              </NativeSelect.Field>
            </NativeSelect.Root>
          </Field.Root>

          <Field.Root>
            <Field.Label>Pago estimado (L)</Field.Label>
            <Input
              type="number"
              value={draft.salary}
              onChange={(event) => onChangeField("salary", event.target.value)}
              placeholder="Ej. 28000"
            />
          </Field.Root>

          <Field.Root>
            <Field.Label>Fecha de cierre</Field.Label>
            <Input
              type="date"
              value={draft.closeDate}
              onChange={(event) => onChangeField("closeDate", event.target.value)}
            />
          </Field.Root>

          <Field.Root>
            <Field.Label>Habilidades requeridas</Field.Label>
            <Input
              value={draft.requiredSkills}
              onChange={(event) => onChangeField("requiredSkills", event.target.value)}
              placeholder="Ej. React, TypeScript, Node"
            />
          </Field.Root>

          <Field.Root>
            <Field.Label>Descripción del trabajo</Field.Label>
            <Textarea
              value={draft.description}
              onChange={(event) => onChangeField("description", event.target.value)}
              placeholder="Responsabilidades, entregables y alcance de la vacante"
              resize="vertical"
            />
          </Field.Root>

          {/* Bloque: acciones del formulario */}
          <ButtonGroup>
            <Button colorPalette="purple" onClick={onSave}>
              {editingId ? "Guardar cambios" : "Publicar vacante"}
            </Button>
            <Button variant="outline" onClick={onReset}>
              Limpiar
            </Button>
          </ButtonGroup>
        </Stack>
      </Card.Body>
    </Card.Root>
  );
}
