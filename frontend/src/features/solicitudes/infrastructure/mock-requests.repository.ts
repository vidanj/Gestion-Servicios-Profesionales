import { ServiceRequest } from "../domain/request.model";

// Datos simulados para trabajar el frontend sin tocar backend.
const initialRequests: ServiceRequest[] = [
  {
    id: "1",
    code: "VAC-1001",
    serviceId: "srv-1",
    serviceName: "Desarrollo de aplicaciones web",
    employerName: "Comercial Rivera",
    jobTitle: "Frontend Developer React",
    category: "Desarrollo Web",
    workMode: "remoto",
    level: "semi_senior",
    status: "en_revision",
    salary: 28000,
    closeDate: "2026-03-10",
    requiredSkills: "React, TypeScript, Chakra UI",
    description: "Construcción de panel de empleadores y flujo de postulaciones.",
    applicantsCount: 14,
    createdAt: "2026-02-12T09:00:00.000Z",
  },
  {
    id: "2",
    code: "VAC-1002",
    serviceId: "srv-2",
    serviceName: "Diseño UI/UX",
    employerName: "Laboratorio Nova",
    jobTitle: "Diseñador UI/UX Freelance",
    category: "Diseño",
    workMode: "hibrido",
    level: "junior",
    status: "publicada",
    salary: 18000,
    closeDate: "2026-03-20",
    requiredSkills: "Figma, Design System, Prototipado",
    description: "Rediseño del portal de servicios y mejora de experiencia móvil.",
    applicantsCount: 8,
    createdAt: "2026-02-16T14:30:00.000Z",
  },
  {
    id: "3",
    code: "VAC-1003",
    serviceId: "srv-3",
    serviceName: "Integración API y backend",
    employerName: "Estudio Méndez",
    jobTitle: "Backend .NET Developer",
    category: "Backend",
    workMode: "presencial",
    level: "senior",
    status: "entrevista",
    salary: 35000,
    closeDate: "2026-04-05",
    requiredSkills: ".NET 8, Entity Framework, SQL Server",
    description: "API para marketplace freelance y módulos de autenticación.",
    applicantsCount: 5,
    createdAt: "2026-02-08T11:15:00.000Z",
  },
];

export function getInitialRequests(): ServiceRequest[] {
  return initialRequests;
}
