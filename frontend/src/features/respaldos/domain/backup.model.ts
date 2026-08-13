// Capa de dominio: tipos y reglas puras. Sin React y sin acceso a red.

export interface Backup {
  fileName: string;
  createdAt: string;
  fileSizeBytes: number;
}

// Mismo patron que aplica el backend en BackupService.OpenBackup. Se replica aqui
// para no llegar a pedir al servidor un nombre que ya sabemos que va a rechazar.
// No es una medida de seguridad: la autoritativa es la del servidor.
const BACKUP_FILE_NAME = /^backup_\d{8}_\d{4}\.sql$/;

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
