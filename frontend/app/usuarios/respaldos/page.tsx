"use client";

import { BackupsPanel } from "@/features/respaldos/presentation/backups-panel";

// La ruta solo monta la pantalla: la logica vive en la feature, segun la regla 8.
export default function RespaldosPage() {
  return <BackupsPanel />;
}
