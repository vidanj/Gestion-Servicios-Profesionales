"use client";

import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import { Button, Field, HStack, Input, Stack, Text } from "@chakra-ui/react";
import {
  profileService,
  type ProfileData,
  type UpdateProfilePayload,
} from "@/services/profile.service";

const schema = yup.object({
  firstName: yup
    .string()
    .required("El nombre es obligatorio")
    .max(255, "Máximo 255 caracteres"),
  lastName: yup
    .string()
    .required("El apellido es obligatorio")
    .max(255, "Máximo 255 caracteres"),
  phoneNumber: yup.string().max(20, "Máximo 20 caracteres").default(""),
});

type FormValues = yup.InferType<typeof schema>;

type ProfileFormProps = {
  profile: ProfileData;
  onUpdate: (updated: ProfileData) => void;
};

export function ProfileForm({ profile, onUpdate }: ProfileFormProps) {
  const [success, setSuccess] = useState("");
  const [apiError, setApiError] = useState("");

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: yupResolver(schema),
    defaultValues: {
      firstName: profile.firstName,
      lastName: profile.lastName,
      phoneNumber: profile.phoneNumber ?? "",
    },
  });

  // Sincronizar si cambia el perfil externo (ej. tras subir foto)
  useEffect(() => {
    reset({
      firstName: profile.firstName,
      lastName: profile.lastName,
      phoneNumber: profile.phoneNumber ?? "",
    });
  }, [profile, reset]);

  const onSubmit = async (values: FormValues) => {
    setSuccess("");
    setApiError("");
    try {
      const payload: UpdateProfilePayload = {
        firstName: values.firstName,
        lastName: values.lastName,
        phoneNumber: values.phoneNumber || undefined,
      };
      const updated = await profileService.updateProfile(payload);
      onUpdate(updated);
      setSuccess("Perfil actualizado correctamente.");
    } catch (err) {
      setApiError(err instanceof Error ? err.message : "Error al guardar");
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate>
      <Stack gap="4">
        <Field.Root invalid={!!errors.firstName}>
          <Field.Label>Nombre</Field.Label>
          <Input {...register("firstName")} placeholder="Juan" />
          {errors.firstName && (
            <Field.ErrorText>{errors.firstName.message}</Field.ErrorText>
          )}
        </Field.Root>

        <Field.Root invalid={!!errors.lastName}>
          <Field.Label>Apellido</Field.Label>
          <Input {...register("lastName")} placeholder="Pérez" />
          {errors.lastName && (
            <Field.ErrorText>{errors.lastName.message}</Field.ErrorText>
          )}
        </Field.Root>

        <Field.Root>
          <Field.Label>Correo electrónico</Field.Label>
          <Input value={profile.email} readOnly opacity={0.5} />
        </Field.Root>

        <Field.Root invalid={!!errors.phoneNumber}>
          <Field.Label>Teléfono</Field.Label>
          <Input {...register("phoneNumber")} type="tel" placeholder="+504 9999-9999" />
          {errors.phoneNumber && (
            <Field.ErrorText>{errors.phoneNumber.message}</Field.ErrorText>
          )}
        </Field.Root>

        {apiError && (
          <Text color="red.400" fontSize="sm">
            {apiError}
          </Text>
        )}
        {success && (
          <Text color="green.400" fontSize="sm">
            {success}
          </Text>
        )}

        <HStack justify="flex-end">
          <Button
            type="submit"
            colorPalette="purple"
            loading={isSubmitting}
          >
            Guardar cambios
          </Button>
        </HStack>
      </Stack>
    </form>
  );
}
