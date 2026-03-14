"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import NextLink from "next/link";
import { Box, Button, Card, Container, Heading, Separator, Stack, Text } from "@chakra-ui/react";
import { ChangePasswordForm } from "@/components/profile/change-password-form";

export default function ContrasenaPage() {
  const router = useRouter();
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
    const token = typeof window !== "undefined" ? localStorage.getItem("token") : null;
    if (!token) router.push("/login");
  }, [router]);

  if (!mounted) return null;

  return (
    <Box
      minH="100vh"
      style={{ background: "linear-gradient(135deg, #0d131b 0%, #1a0a2e 50%, #0d131b 100%)" }}
      py={{ base: 10, md: 16 }}
    >
      <Container maxW="container.xs">

        <Stack gap="1" mb="8">
          <Heading size="lg" style={{ color: "white" }}>Cambiar contraseña</Heading>
          <Text style={{ color: "rgba(255,255,255,0.5)" }}>
            Debes ingresar tu contraseña actual para establecer una nueva.
          </Text>
        </Stack>

        <Card.Root
          style={{
            background: "rgba(255,255,255,0.04)",
            border: "1px solid rgba(255,255,255,0.08)",
            borderRadius: "1.5rem",
          }}
        >
          <Card.Body>
            <ChangePasswordForm />
          </Card.Body>

          <Separator style={{ borderColor: "rgba(255,255,255,0.08)" }} />

          <Card.Footer>
            <Button asChild variant="ghost" size="sm" style={{ color: "rgba(255,255,255,0.5)" }}>
              <NextLink href="/perfil">← Volver al perfil</NextLink>
            </Button>
          </Card.Footer>
        </Card.Root>

      </Container>
    </Box>
  );
}
