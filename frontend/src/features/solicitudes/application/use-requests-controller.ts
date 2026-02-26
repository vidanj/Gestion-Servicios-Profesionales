import { useEffect, useMemo, useState } from "react";

import {
  availableServices,
  createRequestFromDraft,
  emptyRequestDraft,
  filterRequests,
  getNextStatus,
  RequestStatus,
  summarizeRequests,
} from "../domain/request.model";
import { getInitialRequests } from "../infrastructure/mock-requests.repository";

// Hook controlador: maneja estado de la pantalla y coordina acciones UI.
export function useRequestsController() {
  const [requests, setRequests] = useState(getInitialRequests);
  const [draft, setDraft] = useState(emptyRequestDraft);
  const [searchText, setSearchText] = useState("");
  const [statusFilter, setStatusFilter] = useState<"todos" | RequestStatus>("todos");
  const [editingId, setEditingId] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    // Actualización automática de estado para simular seguimiento en tiempo real.
    const intervalId = setInterval(() => {
      setRequests((previous) =>
        previous.map((request) => {
          if (request.status === "cerrada") return request;
          return { ...request, status: getNextStatus(request.status) };
        }),
      );
    }, 20000);

    return () => clearInterval(intervalId);
  }, []);

  const visibleRequests = useMemo(
    () => filterRequests(requests, searchText, statusFilter),
    [requests, searchText, statusFilter],
  );

  const summary = useMemo(() => summarizeRequests(requests), [requests]);

  function setDraftField(field: keyof typeof draft, value: string) {
    if (errorMessage) setErrorMessage("");
    setDraft((previous) => ({ ...previous, [field]: value }));
  }

  function selectService(serviceId: string) {
    const selectedService = availableServices.find((service) => service.id === serviceId);

    setDraft((previous) => ({
      ...previous,
      serviceId,
      category: selectedService?.category || previous.category,
    }));
  }

  function resetDraft() {
    setDraft(emptyRequestDraft);
    setEditingId(null);
    setErrorMessage("");
  }

  function saveRequest() {
    // Validación básica para evitar crear vacantes incompletas.
    if (!draft.serviceId || !draft.employerName.trim() || !draft.jobTitle.trim()) {
      setErrorMessage("Completa servicio, empresa y título de vacante antes de guardar.");
      return;
    }

    if (editingId) {
      setRequests((previous) =>
        previous.map((request) =>
          request.id === editingId
            ? {
                ...request,
                ...createRequestFromDraft(draft, request.id, request.code),
                createdAt: request.createdAt,
                applicantsCount: request.applicantsCount,
              }
            : request,
        ),
      );
      setErrorMessage("");
      resetDraft();
      return;
    }

    const nextNumber = requests.length + 1001;
    const newRequest = createRequestFromDraft(
      draft,
      String(Date.now()),
      `VAC-${nextNumber}`,
    );

    setRequests((previous) => [newRequest, ...previous]);
    setErrorMessage("");
    resetDraft();
  }

  function editRequest(requestId: string) {
    const selected = requests.find((request) => request.id === requestId);
    if (!selected) return;

    setEditingId(selected.id);
    setDraft({
      serviceId: selected.serviceId,
      employerName: selected.employerName,
      jobTitle: selected.jobTitle,
      category: selected.category,
      workMode: selected.workMode,
      level: selected.level,
      status: selected.status,
      salary: selected.salary ? String(selected.salary) : "",
      closeDate: selected.closeDate,
      requiredSkills: selected.requiredSkills,
      description: selected.description,
    });
  }

  function removeRequest(requestId: string) {
    setRequests((previous) => previous.filter((request) => request.id !== requestId));
    if (editingId === requestId) resetDraft();
  }

  function updateStatus(requestId: string, status: RequestStatus) {
    setRequests((previous) =>
      previous.map((request) => (request.id === requestId ? { ...request, status } : request)),
    );
  }

  return {
    draft,
    editingId,
    requests: visibleRequests,
    searchText,
    statusFilter,
    summary,
    errorMessage,
    selectService,
    setDraftField,
    setSearchText,
    setStatusFilter,
    saveRequest,
    editRequest,
    removeRequest,
    updateStatus,
    resetDraft,
  };
}
