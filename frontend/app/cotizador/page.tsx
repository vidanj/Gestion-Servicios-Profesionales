"use client";

import { FormEvent, useState } from "react";
import {
  Alert,
  Box,
  Button,
  Card,
  Checkbox,
  Container,
  Field,
  Heading,
  HStack,
  Input,
  NativeSelect,
  Separator,
  SimpleGrid,
  Stack,
  Text,
} from "@chakra-ui/react";

// Estas interfaces representan exactamente lo que espera/devuelve el backend.
interface PriceEstimateRequest {
  basePrice: number;
  complexityLevel: "baja" | "media" | "alta";
  urgencyLevel: "normal" | "urgente" | "express";
  extraRevisions: number;
  includePrioritySupport: boolean;
  includeWeekendDelivery: boolean;
}

interface PriceEstimateResponse {
  basePrice: number;
  complexityModifierPercent: number;
  urgencyModifierPercent: number;
  revisionsModifierPercent: number;
  prioritySupportFee: number;
  weekendDeliveryFee: number;
  estimatedTotal: number;
  notes: string[];
}

// Si no existe variable de entorno, usamos el backend local de ASP.NET.
const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5000";

export default function CotizadorPage() {
  const [basePrice, setBasePrice] = useState("250");
  const [complexityLevel, setComplexityLevel] = useState<"baja" | "media" | "alta">("media");
  const [urgencyLevel, setUrgencyLevel] = useState<"normal" | "urgente" | "express">("normal");
  const [extraRevisions, setExtraRevisions] = useState("0");
  const [includePrioritySupport, setIncludePrioritySupport] = useState(false);
  const [includeWeekendDelivery, setIncludeWeekendDelivery] = useState(false);

  const [loading, setLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [result, setResult] = useState<PriceEstimateResponse | null>(null);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setErrorMessage("");
    setLoading(true);

    const payload: PriceEstimateRequest = {
      basePrice: Number(basePrice),
      complexityLevel,
      urgencyLevel,
      extraRevisions: Number(extraRevisions),
      includePrioritySupport,
      includeWeekendDelivery,
    };

    if (Number.isNaN(payload.basePrice) || payload.basePrice <= 0) {
      setErrorMessage("El precio base debe ser mayor que 0.");
      setLoading(false);
      return;
    }

    try {
      const response = await fetch(`${API_BASE_URL}/api/pricing/estimate`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(payload),
      });

      if (!response.ok) {
        throw new Error("No se pudo calcular el precio estimado.");
      }

      const data = (await response.json()) as PriceEstimateResponse;
      setResult(data);
    } catch {
      setErrorMessage(
        "No pude conectarme con el servidor. Verifica que la API esté disponible.",
      );
    } finally {
      setLoading(false);
    }
  }

  return (
    <Box py={{ base: 10, md: 16 }}>
      <Container maxW="container.xl">
        <Stack gap="6">
          <Stack gap="2">
            <Heading size="lg">Cotizador de servicios</Heading>
            <Text color="muted">
              Calcula dinámicamente un precio estimado con base en complejidad, urgencia y extras.
            </Text>
          </Stack>

          <SimpleGrid columns={{ base: 1, lg: 2 }} gap="6">
            <Card.Root>
              <Card.Header>
                <Heading size="md">Datos de la cotización</Heading>
              </Card.Header>
              <Card.Body>
                <form onSubmit={handleSubmit}>
                  <Stack gap="4">
                    <Field.Root>
                      <Field.Label>Precio base del profesional</Field.Label>
                      <Input
                        data-testid="cotizador-precio-base"
                        type="number"
                        value={basePrice}
                        onChange={(event) => setBasePrice(event.target.value)}
                        placeholder="Ej: 250"
                      />
                    </Field.Root>

                    <Field.Root>
                      <Field.Label>Complejidad</Field.Label>
                      <NativeSelect.Root>
                        <NativeSelect.Field
                          data-testid="cotizador-complejidad"
                          value={complexityLevel}
                          onChange={(event) =>
                            setComplexityLevel(event.target.value as "baja" | "media" | "alta")
                          }
                        >
                          <option value="baja">Baja</option>
                          <option value="media">Media</option>
                          <option value="alta">Alta</option>
                        </NativeSelect.Field>
                      </NativeSelect.Root>
                    </Field.Root>

                    <Field.Root>
                      <Field.Label>Urgencia</Field.Label>
                      <NativeSelect.Root>
                        <NativeSelect.Field
                          data-testid="cotizador-urgencia"
                          value={urgencyLevel}
                          onChange={(event) =>
                            setUrgencyLevel(event.target.value as "normal" | "urgente" | "express")
                          }
                        >
                          <option value="normal">Normal</option>
                          <option value="urgente">Urgente</option>
                          <option value="express">Express</option>
                        </NativeSelect.Field>
                      </NativeSelect.Root>
                    </Field.Root>

                    <Field.Root>
                      <Field.Label>Revisiones extra</Field.Label>
                      <Input
                        data-testid="cotizador-revisiones"
                        type="number"
                        value={extraRevisions}
                        onChange={(event) => setExtraRevisions(event.target.value)}
                        min={0}
                        max={10}
                      />
                    </Field.Root>

                    <Stack gap="3">
                      <Checkbox.Root
                        data-testid="cotizador-soporte"
                        checked={includePrioritySupport}
                        onCheckedChange={(details) => setIncludePrioritySupport(details.checked === true)}
                      >
                        <Checkbox.HiddenInput />
                        <Checkbox.Control />
                        <Checkbox.Label>Soporte prioritario (+$35)</Checkbox.Label>
                      </Checkbox.Root>

                      <Checkbox.Root
                        data-testid="cotizador-fds"
                        checked={includeWeekendDelivery}
                        onCheckedChange={(details) => setIncludeWeekendDelivery(details.checked === true)}
                      >
                        <Checkbox.HiddenInput />
                        <Checkbox.Control />
                        <Checkbox.Label>Entrega fin de semana (+$20)</Checkbox.Label>
                      </Checkbox.Root>
                    </Stack>

                    <HStack justify="flex-end">
                      <Button data-testid="cotizador-submit" type="submit" colorPalette="purple" loading={loading}>
                        Calcular estimado
                      </Button>
                    </HStack>
                  </Stack>
                </form>
              </Card.Body>
            </Card.Root>

            <Card.Root variant="outline">
              <Card.Header>
                <Heading size="md">Resultado del cálculo</Heading>
              </Card.Header>
              <Card.Body>
                <Stack gap="3">
                  {errorMessage ? (
                    <Alert.Root status="error">
                      <Alert.Indicator />
                      <Alert.Content>
                        <Alert.Title data-testid="cotizador-error">{errorMessage}</Alert.Title>
                      </Alert.Content>
                    </Alert.Root>
                  ) : null}

                  {!result ? (
                    <Text color="muted">Completa el formulario y presiona “Calcular estimado”.</Text>
                  ) : (
                    <>
                      <SimpleGrid columns={2} gap="3">
                        <Text color="muted">Precio base</Text>
                        <Text textAlign="right">${result.basePrice.toFixed(2)}</Text>

                        <Text color="muted">Complejidad</Text>
                        <Text textAlign="right">{(result.complexityModifierPercent * 100).toFixed(0)}%</Text>

                        <Text color="muted">Urgencia</Text>
                        <Text textAlign="right">{(result.urgencyModifierPercent * 100).toFixed(0)}%</Text>

                        <Text color="muted">Revisiones</Text>
                        <Text textAlign="right">{(result.revisionsModifierPercent * 100).toFixed(0)}%</Text>

                        <Text color="muted">Soporte prioritario</Text>
                        <Text textAlign="right">${result.prioritySupportFee.toFixed(2)}</Text>

                        <Text color="muted">Entrega fin de semana</Text>
                        <Text textAlign="right">${result.weekendDeliveryFee.toFixed(2)}</Text>
                      </SimpleGrid>

                      <Separator my="2" />

                      <HStack justify="space-between">
                        <Heading size="sm">Total estimado</Heading>
                        <Heading data-testid="cotizador-total" size="md" color="purple.500">
                          ${result.estimatedTotal.toFixed(2)}
                        </Heading>
                      </HStack>

                      <Separator my="2" />

                      <Stack gap="1">
                        <Text fontWeight="semibold">Notas:</Text>
                        {result.notes.map((note) => (
                          <Text key={note} fontSize="sm" color="muted">
                            • {note}
                          </Text>
                        ))}
                      </Stack>
                    </>
                  )}
                </Stack>
              </Card.Body>
            </Card.Root>
          </SimpleGrid>

        </Stack>
      </Container>
    </Box>
  );
}
