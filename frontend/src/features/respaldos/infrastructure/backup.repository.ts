import { backupService, BackupJobData } from "@/services/backup.service";

import {
  Backup,
  BackupJob,
  esEstadoConocido,
  sortBackupsByDateDesc,
} from "../domain/backup.model";

// Punto unico donde la respuesta cruda se convierte en algo que el dominio entiende.
// Si el estado no se reconoce se corta aqui: dejarlo pasar lo convertiria en un sondeo
// que nunca termina, que es justo el fallo que se vio en produccion.
function aTrabajo(raw: BackupJobData): BackupJob {
  if (!esEstadoConocido(raw.status)) {
    throw new Error(
      `El servidor devolvió un estado de respaldo no reconocido (${JSON.stringify(raw.status)}). ` +
        "Puede que el respaldo se haya generado: recarga la lista para comprobarlo.",
    );
  }

  return {
    id: raw.id,
    status: raw.status,
    fileName: raw.fileName,
    error: raw.error,
  };
}

// Capa de infraestructura: traduce entre el servicio HTTP y el dominio.
// La capa de aplicacion depende de este puerto, no de fetch.
export interface BackupRepository {
  list(): Promise<Backup[]>;
  generate(): Promise<BackupJob>;
  getJob(jobId: string): Promise<BackupJob>;
  download(fileName: string): Promise<Blob>;
}

export const httpBackupRepository: BackupRepository = {
  async list() {
    const data = await backupService.listBackups();
    return sortBackupsByDateDesc(data);
  },

  // Devuelve el trabajo encolado, no el archivo: el respaldo aun no existe.
  async generate() {
    return aTrabajo(await backupService.generateBackup());
  },

  async getJob(jobId: string) {
    return aTrabajo(await backupService.getBackupJob(jobId));
  },

  async download(fileName: string) {
    return backupService.downloadBackup(fileName);
  },
};
