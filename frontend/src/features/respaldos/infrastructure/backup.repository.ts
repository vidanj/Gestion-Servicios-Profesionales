import { backupService } from "@/services/backup.service";

import { Backup, BackupJob, sortBackupsByDateDesc } from "../domain/backup.model";

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
    return backupService.generateBackup();
  },

  async getJob(jobId: string) {
    return backupService.getBackupJob(jobId);
  },

  async download(fileName: string) {
    return backupService.downloadBackup(fileName);
  },
};
