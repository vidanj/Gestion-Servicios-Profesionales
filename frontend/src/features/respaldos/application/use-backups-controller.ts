import { useCallback, useEffect, useRef, useState } from "react";

import { Backup, esEstadoFinal, isValidBackupFileName } from "../domain/backup.model";
import { BackupRepository, httpBackupRepository } from "../infrastructure/backup.repository";

/** Cada cuánto se pregunta por el estado del trabajo. */
const INTERVALO_SONDEO_MS = 1500;

/** Tope del sondeo: sin él, la pantalla giraría para siempre si el servidor calla. */
const MAX_INTENTOS_SONDEO = 60;

const esperar = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));

// Hook controlador: mantiene el estado de la pantalla y coordina las acciones.
// El repositorio se inyecta para poder sustituirlo en pruebas.
export function useBackupsController(repository: BackupRepository = httpBackupRepository) {
  const [backups, setBackups] = useState<Backup[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isGenerating, setIsGenerating] = useState(false);
  const [downloadingFile, setDownloadingFile] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState("");

  // Evita que un sondeo en curso siga escribiendo estado tras desmontar la pantalla.
  const montado = useRef(true);
  useEffect(() => {
    montado.current = true;
    return () => {
      montado.current = false;
    };
  }, []);

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
    // La generacion es asincrona en el servidor: sin este guardia, un doble clic
    // encolaria dos peticiones aunque el backend devuelva el mismo trabajo.
    if (isGenerating) return;

    setIsGenerating(true);
    setErrorMessage("");

    try {
      // El POST solo encola: responde 202 con un trabajo todavia sin terminar.
      const job = await repository.generate();

      // Hay que esperar a que termine de verdad. Recargar la lista aqui mostraria
      // el estado anterior, porque el archivo aun no existe.
      let estado = job;
      for (let intento = 0; !esEstadoFinal(estado.status); intento++) {
        if (intento >= MAX_INTENTOS_SONDEO) {
          setErrorMessage(
            "El respaldo está tardando más de lo esperado. Revisa la lista en unos minutos.",
          );
          return;
        }

        await esperar(INTERVALO_SONDEO_MS);
        if (!montado.current) return;

        estado = await repository.getJob(job.id);
      }

      if (estado.status === "Fallido") {
        setErrorMessage(estado.error ?? "No se pudo generar el respaldo.");
        return;
      }

      await loadBackups();
    } catch (error) {
      setErrorMessage(
        error instanceof Error ? error.message : "Error al generar el respaldo",
      );
    } finally {
      if (montado.current) setIsGenerating(false);
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
