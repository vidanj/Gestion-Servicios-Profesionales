export type RequestStatus = "publicada" | "en_revision" | "entrevista" | "cerrada";

export type WorkMode = "remoto" | "hibrido" | "presencial";

export type ExperienceLevel = "junior" | "semi_senior" | "senior";

export interface ServiceOption {
  id: string;
  name: string;
  category: string;
}

export const availableServices: ServiceOption[] = [
  { id: "srv-1", name: "Desarrollo de aplicaciones web", category: "Desarrollo Web" },
  { id: "srv-2", name: "Diseño UI/UX", category: "Diseño" },
  { id: "srv-3", name: "Integración API y backend", category: "Backend" },
  { id: "srv-4", name: "Marketing digital", category: "Marketing" },
];

export interface ServiceRequest {
  id: string;
  code: string;
  serviceId: string;
  serviceName: string;
  employerName: string;
  jobTitle: string;
  category: string;
  workMode: WorkMode;
  level: ExperienceLevel;
  status: RequestStatus;
  salary: number;
  closeDate: string;
  requiredSkills: string;
  description: string;
  applicantsCount: number;
  createdAt: string;
}

export interface RequestDraft {
  serviceId: string;
  employerName: string;
  jobTitle: string;
  category: string;
  workMode: WorkMode;
  level: ExperienceLevel;
  status: RequestStatus;
  salary: string;
  closeDate: string;
  requiredSkills: string;
  description: string;
}

export interface RequestSummary {
  total: number;
  published: number;
  reviewing: number;
  interviewing: number;
  closed: number;
}

export const emptyRequestDraft: RequestDraft = {
  serviceId: availableServices[0].id,
  employerName: "",
  jobTitle: "",
  category: "Desarrollo Web",
  workMode: "remoto",
  level: "junior",
  status: "publicada",
  salary: "",
  closeDate: "",
  requiredSkills: "",
  description: "",
};

export const statusLabel: Record<RequestStatus, string> = {
  publicada: "Publicada",
  en_revision: "En revisión",
  entrevista: "En entrevista",
  cerrada: "Cerrada",
};

export const workModeLabel: Record<WorkMode, string> = {
  remoto: "Remoto",
  hibrido: "Híbrido",
  presencial: "Presencial",
};

export const levelLabel: Record<ExperienceLevel, string> = {
  junior: "Junior",
  semi_senior: "Semi Senior",
  senior: "Senior",
};

export function createRequestFromDraft(
  draft: RequestDraft,
  id: string,
  code: string,
): ServiceRequest {
  const selectedService = availableServices.find((service) => service.id === draft.serviceId);

  return {
    id,
    code,
    serviceId: draft.serviceId,
    serviceName: selectedService?.name || "Servicio general",
    employerName: draft.employerName.trim(),
    jobTitle: draft.jobTitle.trim(),
    category: draft.category,
    workMode: draft.workMode,
    level: draft.level,
    status: draft.status,
    salary: Number(draft.salary || 0),
    closeDate: draft.closeDate,
    requiredSkills: draft.requiredSkills.trim(),
    description: draft.description.trim(),
    applicantsCount: 0,
    createdAt: new Date().toISOString(),
  };
}

export function getNextStatus(currentStatus: RequestStatus): RequestStatus {
  if (currentStatus === "publicada") return "en_revision";
  if (currentStatus === "en_revision") return "entrevista";
  return currentStatus;
}

export function summarizeRequests(requests: ServiceRequest[]): RequestSummary {
  return {
    total: requests.length,
    published: requests.filter((request) => request.status === "publicada").length,
    reviewing: requests.filter((request) => request.status === "en_revision").length,
    interviewing: requests.filter((request) => request.status === "entrevista").length,
    closed: requests.filter((request) => request.status === "cerrada").length,
  };
}

export function filterRequests(
  requests: ServiceRequest[],
  searchText: string,
  status: "todos" | RequestStatus,
): ServiceRequest[] {
  const cleanSearch = searchText.trim().toLowerCase();

  return requests.filter((request) => {
    const matchesStatus = status === "todos" ? true : request.status === status;
    const matchesSearch =
      cleanSearch.length === 0
        ? true
        : request.employerName.toLowerCase().includes(cleanSearch) ||
          request.jobTitle.toLowerCase().includes(cleanSearch) ||
          request.serviceName.toLowerCase().includes(cleanSearch) ||
          request.requiredSkills.toLowerCase().includes(cleanSearch) ||
          request.code.toLowerCase().includes(cleanSearch);

    return matchesStatus && matchesSearch;
  });
}
