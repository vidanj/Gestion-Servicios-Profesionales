// Capa de dominio: tipos y reglas puras. Sin React y sin acceso a red.

export interface Backup {
  fileName: string;
  createdAt: string;
  fileSizeBytes: number;
}

export type BackupJobStatus = "Pendiente" | "EnProceso" | "Completado" | "Fallido";

export interface BackupJob {
  id: string;
  status: BackupJobStatus;
  fileName?: string;
  error?: string;
}

/** Un trabajo terminado ya no cambia de estado: no tiene sentido seguir sondeando. */
export function esEstadoFinal(status: BackupJobStatus): boolean {
  return status === "Completado" || status === "Fallido";
}

const ESTADOS: readonly string[] = ["Pendiente", "EnProceso", "Completado", "Fallido"];

/**
 * Comprueba que el estado que llegó del servidor es uno de los que esta pantalla entiende.
 *
 * El backend serializaba el estado como número y aquí se comparaba contra nombres, así que
 * `esEstadoFinal` devolvía siempre false y el sondeo agotaba el tope mientras el respaldo
 * ya estaba hecho. Lo peor no fue el desajuste sino cómo falló: en silencio y durante minuto
 * y medio. Un estado que no se reconoce tiene que notarse de inmediato.
 */
export function esEstadoConocido(status: unknown): status is BackupJobStatus {
  return typeof status === "string" && ESTADOS.includes(status);
}

// Mismo patron que aplica el backend en BackupService.OpenBackup. Se replica aqui
// para no llegar a pedir al servidor un nombre que ya sabemos que va a rechazar.
// No es una medida de seguridad: la autoritativa es la del servidor.
// Seis digitos es el formato actual (HHmmss); cuatro es el anterior (HHmm), que se
// sigue aceptando para que los respaldos ya generados se puedan descargar. El sufijo
// opcional desambigua dos respaldos creados dentro del mismo segundo.
const BACKUP_FILE_NAME = /^backup_\d{8}_(\d{6}|\d{4})(_\d+)?\.sql$/;

export function isValidBackupFileName(fileName: string): boolean {
  return BACKUP_FILE_NAME.test(fileName);
}

const UNITS = ["B", "KB", "MB", "GB"] as const;

export function formatFileSize(bytes: number): string {
  if (!Number.isFinite(bytes) || bytes < 0) return "—";
  if (bytes === 0) return "0 B";

  let value = bytes;
  let unitIndex = 0;

  while (value >= 1024 && unitIndex < UNITS.length - 1) {
    value /= 1024;
    unitIndex += 1;
  }

  // Sin decimales para bytes; con uno para el resto, que basta para comparar respaldos.
  const rounded = unitIndex === 0 ? String(value) : value.toFixed(1);
  return `${rounded} ${UNITS[unitIndex]}`;
}

export function formatBackupDate(isoDate: string): string {
  const date = new Date(isoDate);
  if (Number.isNaN(date.getTime())) return "—";

  return date.toLocaleString("es-HN", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });
}

// El backend ya devuelve la lista ordenada, pero la vista no debe depender de ello.
export function sortBackupsByDateDesc(backups: Backup[]): Backup[] {
  return [...backups].sort(
    (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
  );
}
