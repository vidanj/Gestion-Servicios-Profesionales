"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import { Button, HStack, Stack, Text } from "@chakra-ui/react";
import { profileService } from "@/services/profile.service";

const schema = yup.object({
  currentPassword: yup.string().required("La contraseña actual es obligatoria"),
  newPassword: yup
    .string()
    .min(8, "Mínimo 8 caracteres")
    .required("La nueva contraseña es obligatoria"),
  confirmNewPassword: yup
    .string()
    .oneOf([yup.ref("newPassword")], "Las contraseñas no coinciden")
    .required("Confirma la nueva contraseña"),
});

type FormValues = yup.InferType<typeof schema>;

const inputStyle: React.CSSProperties = {
  width: "100%",
  background: "rgba(255,255,255,0.06)",
  border: "1px solid rgba(255,255,255,0.14)",
  borderRadius: "0.6rem",
  padding: "0.6rem 0.85rem",
  color: "white",
  fontSize: "0.95rem",
  outline: "none",
};

const labelStyle: React.CSSProperties = {
  color: "rgba(255,255,255,0.55)",
  fontSize: "0.78rem",
  marginBottom: "0.3rem",
  letterSpacing: "0.04em",
  textTransform: "uppercase",
  display: "block",
};

export function ChangePasswordForm() {
  const [success, setSuccess] = useState("");
  const [apiError, setApiError] = useState("");

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: yupResolver(schema),
  });

  const onSubmit = async (values: FormValues) => {
    setSuccess("");
    setApiError("");
    try {
      await profileService.changePassword({
        currentPassword: values.currentPassword,
        newPassword: values.newPassword,
        confirmNewPassword: values.confirmNewPassword,
      });
      setSuccess("Contraseña actualizada correctamente.");
      reset();
    } catch (err) {
      setApiError(err instanceof Error ? err.message : "Error al cambiar la contraseña");
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate>
      <Stack gap="5">

        <div>
          <label style={labelStyle}>Contraseña actual</label>
          <input data-testid="current-password-input" {...register("currentPassword")} type="password" placeholder="••••••••" style={inputStyle} />
          {errors.currentPassword && (
            <p style={{ color: "#f87171", fontSize: "0.78rem", marginTop: "0.3rem" }}>
              {errors.currentPassword.message}
            </p>
          )}
        </div>

        <div>
          <label style={labelStyle}>Nueva contraseña</label>
          <input data-testid="new-password-input" {...register("newPassword")} type="password" placeholder="Mínimo 8 caracteres" style={inputStyle} />
          {errors.newPassword && (
            <p style={{ color: "#f87171", fontSize: "0.78rem", marginTop: "0.3rem" }}>
              {errors.newPassword.message}
            </p>
          )}
        </div>

        <div>
          <label style={labelStyle}>Confirmar nueva contraseña</label>
          <input data-testid="confirm-password-input" {...register("confirmNewPassword")} type="password" placeholder="••••••••" style={inputStyle} />
          {errors.confirmNewPassword && (
            <p style={{ color: "#f87171", fontSize: "0.78rem", marginTop: "0.3rem" }}>
              {errors.confirmNewPassword.message}
            </p>
          )}
        </div>

        {apiError && <Text style={{ color: "#f87171" }} fontSize="sm">{apiError}</Text>}
        {success && <Text style={{ color: "#34d399" }} fontSize="sm">{success}</Text>}

        <HStack justify="flex-end">
          <Button data-testid="submit-password-btn" type="submit" colorPalette="purple" loading={isSubmitting}>
            Actualizar contraseña
          </Button>
        </HStack>
      </Stack>
    </form>
  );
}
