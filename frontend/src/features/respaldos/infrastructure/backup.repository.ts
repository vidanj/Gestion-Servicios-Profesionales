import { backupService } from "@/services/backup.service";

import { Backup, sortBackupsByDateDesc } from "../domain/backup.model";

// Capa de infraestructura: traduce entre el servicio HTTP y el dominio.
// La capa de aplicacion depende de este puerto, no de fetch.
export interface BackupRepository {
  list(): Promise<Backup[]>;
  generate(): Promise<Backup>;
  download(fileName: string): Promise<Blob>;
}

export const httpBackupRepository: BackupRepository = {
  async list() {
    const data = await backupService.listBackups();
    return sortBackupsByDateDesc(data);
  },

  async generate() {
    return backupService.generateBackup();
  },

  async download(fileName: string) {
    return backupService.downloadBackup(fileName);
  },
};
