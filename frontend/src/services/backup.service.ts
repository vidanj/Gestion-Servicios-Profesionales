const apiUrl = process.env.NEXT_PUBLIC_ALLOWED_PATH;

const getToken = () =>
  typeof window !== "undefined" ? localStorage.getItem("token") : null;

const authHeaders = () => ({
  "Content-Type": "application/json",
  Authorization: `Bearer ${getToken()}`,
});

export type BackupData = {
  fileName: string;
  createdAt: string;
  fileSizeBytes: number;
};

/** Estados que devuelve el backend para un trabajo de respaldo. */
export type BackupJobStatus = "Pendiente" | "EnProceso" | "Completado" | "Fallido";

export type BackupJobData = {
  id: string;
  status: BackupJobStatus;
  fileName?: string;
  fileSizeBytes?: number;
  error?: string;
};

export const backupService = {
  async listBackups(): Promise<BackupData[]> {
    const res = await fetch(`${apiUrl}/api/Admin/backups`, {
      headers: authHeaders(),
    });
    if (!res.ok) throw new Error("No se pudieron cargar los respaldos");
    return res.json();
  },

  // Devuelve un trabajo, no el archivo: el respaldo corre en segundo plano y hay
  // que consultar su estado con getBackupJob hasta que termine.
  async generateBackup(): Promise<BackupJobData> {
    const res = await fetch(`${apiUrl}/api/Admin/backup`, {
      method: "POST",
      headers: authHeaders(),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message ?? "Error al generar el respaldo");
    }
    return res.json();
  },

  async getBackupJob(jobId: string): Promise<BackupJobData> {
    const res = await fetch(`${apiUrl}/api/Admin/backup/jobs/${jobId}`, {
      headers: authHeaders(),
    });
    if (!res.ok) throw new Error("No se pudo consultar el estado del respaldo");
    return res.json();
  },

  // La descarga no puede ser un <a href> normal: el endpoint exige la cabecera
  // Authorization y el navegador no la adjunta en una navegacion. Se pide con
  // fetch y se devuelve el Blob para que la capa superior lo materialice.
  async downloadBackup(fileName: string): Promise<Blob> {
    const res = await fetch(
      `${apiUrl}/api/Admin/backups/${encodeURIComponent(fileName)}`,
      { headers: { Authorization: `Bearer ${getToken()}` } },
    );
    if (!res.ok) throw new Error("No se pudo descargar el respaldo");
    return res.blob();
  },
};
