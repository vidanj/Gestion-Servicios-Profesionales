"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import NextLink from "next/link";
import {
  Box,
  Button,
  Card,
  Container,
  Heading,
  Separator,
  Stack,
  Text,
} from "@chakra-ui/react";
import { FiEdit2 } from "react-icons/fi";
import { AvatarUploader } from "@/components/profile/avatar-uploader";
import { EditableField } from "@/components/profile/editable-field";
import { profileService, type ProfileData } from "@/services/profile.service";
import { useAuthStore } from "@/store/auth-store";

const roleLabel: Record<number, string> = {
  0: "Administrador",
  1: "Cliente",
  2: "Profesionista",
};

export default function PerfilPage() {
  const router = useRouter();
  const setFromProfile = useAuthStore((s) => s.setFromProfile);
  const [mounted, setMounted] = useState(false);
  const [profile, setProfile] = useState<ProfileData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  // Sincronizar el store cada vez que el perfil cambia (carga inicial o actualización)
  useEffect(() => {
    if (profile) setFromProfile(profile);
  }, [profile, setFromProfile]);

  useEffect(() => {
    setMounted(true);
    const token = typeof window !== "undefined" ? localStorage.getItem("token") : null;
    if (!token) {
      router.push("/login");
      return;
    }
    profileService
      .getProfile()
      .then(setProfile)
      .catch(() => setError("No se pudo cargar el perfil."))
      .finally(() => setLoading(false));
  }, [router]);

  if (!mounted) return null;

  const saveField = async (field: "firstName" | "lastName" | "phoneNumber", value: string) => {
    if (!profile) return;
    const updated = await profileService.updateProfile({
      firstName: field === "firstName" ? value : profile.firstName,
      lastName: field === "lastName" ? value : profile.lastName,
      phoneNumber: field === "phoneNumber" ? (value || undefined) : (profile.phoneNumber || undefined),
    });
    setProfile(updated);
  };

  return (
    <Box
      minH="100vh"
      style={{ background: "linear-gradient(135deg, #0d131b 0%, #1a0a2e 50%, #0d131b 100%)" }}
      py={{ base: 10, md: 16 }}
    >
      <Container maxW="container.sm">

        <Stack gap="1" mb="8">
          <Heading size="lg" style={{ color: "white" }}>Mi Perfil</Heading>
          <Text style={{ color: "rgba(255,255,255,0.5)" }}>
            Haz clic en el ícono <FiEdit2 style={{ display: "inline", verticalAlign: "middle" }} /> junto a un campo para editarlo.
          </Text>
        </Stack>

        {loading && (
          <Text style={{ color: "rgba(255,255,255,0.5)" }} textAlign="center" py="12">
            Cargando perfil…
          </Text>
        )}

        {error && (
          <Text style={{ color: "#f87171" }} textAlign="center" py="12">{error}</Text>
        )}

        {profile && (
          <Card.Root
            style={{
              background: "rgba(255,255,255,0.04)",
              border: "1px solid rgba(255,255,255,0.08)",
              borderRadius: "1.5rem",
            }}
          >
            {/* Avatar + nombre + rol */}
            <Card.Header>
              <Stack align="center" gap="3">
                <AvatarUploader
                  profileImageUrl={profile.profileImageUrl}
                  firstName={profile.firstName}
                  onUpdate={setProfile}
                />
                <Stack gap="0" textAlign="center">
                  <Heading size="md" style={{ color: "white" }}>
                    {profile.firstName} {profile.lastName}
                  </Heading>
                  <Text fontSize="sm" style={{ color: "#c4b5fd" }}>
                    {roleLabel[profile.role] ?? "Usuario"}
                  </Text>
                </Stack>
              </Stack>
            </Card.Header>

            <Separator style={{ borderColor: "rgba(255,255,255,0.08)" }} />

            {/* Campos editables */}
            <Card.Body>
              <Stack gap="5">

                <EditableField
                  label="Nombre"
                  value={profile.firstName}
                  onSave={(v) => saveField("firstName", v)}
                />

                <EditableField
                  label="Apellido"
                  value={profile.lastName}
                  onSave={(v) => saveField("lastName", v)}
                />

                {/* Email — solo lectura, sin botón */}
                <div>
                  <p style={{ color: "rgba(255,255,255,0.55)", fontSize: "0.78rem", marginBottom: "0.3rem", letterSpacing: "0.04em", textTransform: "uppercase" }}>
                    Correo electrónico
                  </p>
                  <input
                    readOnly
                    value={profile.email}
                    style={{
                      width: "100%",
                      background: "transparent",
                      border: "1px solid rgba(255,255,255,0.08)",
                      borderRadius: "0.6rem",
                      padding: "0.55rem 0.85rem",
                      color: "rgba(255,255,255,0.4)",
                      fontSize: "0.95rem",
                      outline: "none",
                      cursor: "default",
                    }}
                  />
                </div>

                <EditableField
                  label="Teléfono"
                  value={profile.phoneNumber ?? ""}
                  type="tel"
                  onSave={(v) => saveField("phoneNumber", v)}
                />

              </Stack>
            </Card.Body>

            <Separator style={{ borderColor: "rgba(255,255,255,0.08)" }} />

            {/* Enlace a cambiar contraseña */}
            <Card.Footer>
              <Button asChild variant="ghost" size="sm" style={{ color: "#c4b5fd" }}>
                <NextLink href="/perfil/contrasena">
                  Cambiar contraseña →
                </NextLink>
              </Button>
            </Card.Footer>
          </Card.Root>
        )}
      </Container>
    </Box>
  );
}
