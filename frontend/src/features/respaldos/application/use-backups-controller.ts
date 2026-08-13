import { useCallback, useEffect, useState } from "react";

import { Backup, isValidBackupFileName } from "../domain/backup.model";
import { BackupRepository, httpBackupRepository } from "../infrastructure/backup.repository";

// Hook controlador: mantiene el estado de la pantalla y coordina las acciones.
// El repositorio se inyecta para poder sustituirlo en pruebas.
export function useBackupsController(repository: BackupRepository = httpBackupRepository) {
  const [backups, setBackups] = useState<Backup[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isGenerating, setIsGenerating] = useState(false);
  const [downloadingFile, setDownloadingFile] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState("");

  const loadBackups = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage("");
    try {
      setBackups(await repository.list());
    } catch (error) {
      setErrorMessage(
        error instanceof Error ? error.message : "No se pudieron cargar los respaldos",
      );
    } finally {
      setIsLoading(false);
    }
  }, [repository]);

  useEffect(() => {
    void loadBackups();
  }, [loadBackups]);

  async function generateBackup() {
    // La generacion es sincrona en el servidor y puede tardar: sin este guardia,
    // un doble clic dispara dos pg_dump concurrentes.
    if (isGenerating) return;

    setIsGenerating(true);
    setErrorMessage("");
    try {
      await repository.generate();
      await loadBackups();
    } catch (error) {
      setErrorMessage(
        error instanceof Error ? error.message : "Error al generar el respaldo",
      );
    } finally {
      setIsGenerating(false);
    }
  }

  async function downloadBackup(fileName: string) {
    if (!isValidBackupFileName(fileName)) {
      setErrorMessage("El nombre del respaldo no es válido.");
      return;
    }

    setDownloadingFile(fileName);
    setErrorMessage("");

    let objectUrl: string | null = null;
    try {
      const blob = await repository.download(fileName);

      // El endpoint exige cabecera Authorization, asi que no sirve un enlace directo:
      // se materializa el Blob en un object URL y se dispara un ancla sintetica.
      objectUrl = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = objectUrl;
      anchor.download = fileName;
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
    } catch (error) {
      setErrorMessage(
        error instanceof Error ? error.message : "No se pudo descargar el respaldo",
      );
    } finally {
      // Revocar siempre: el object URL retiene el Blob en memoria hasta liberarlo.
      if (objectUrl) URL.revokeObjectURL(objectUrl);
      setDownloadingFile(null);
    }
  }

  return {
    backups,
    isLoading,
    isGenerating,
    downloadingFile,
    errorMessage,
    generateBackup,
    downloadBackup,
    reload: loadBackups,
  };
}
