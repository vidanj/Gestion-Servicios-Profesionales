"use client";

import { useCallback, useEffect, useState } from "react";

export type UserRecord = {
  id: string;
  name: string;
  email: string;
  username?: string;
  role: string;
  status?: string;
  lastLogin?: string;
  team?: string;
  permissions?: string[];
};

const STORAGE_KEY = "gsp_users_v1";

const initialUsers: UserRecord[] = [
  {
    id: "USR-1024",
    name: "Andrea Torres",
    email: "andrea.torres@empresa.com",
    username: "atorres",
    role: "admin",
    status: "activo",
    lastLogin: "2026-02-10 09:24",
    team: "Operacion",
    permissions: ["usuarios:editar", "reportes:ver", "roles:gestionar"],
  },
  {
    id: "USR-1025",
    name: "Bruno Castillo",
    email: "bruno.castillo@empresa.com",
    username: "bcastillo",
    role: "manager",
    status: "activo",
    lastLogin: "2026-02-12 17:45",
    team: "Ventas",
    permissions: ["clientes:ver", "servicios:editar", "reportes:ver"],
  },
  {
    id: "USR-1026",
    name: "Camila Diaz",
    email: "camila.diaz@empresa.com",
    username: "cdiaz",
    role: "usuario",
    status: "suspendido",
    lastLogin: "2026-01-28 11:05",
    team: "Legal",
    permissions: ["tickets:crear", "perfil:editar"],
  },
  {
    id: "USR-1027",
    name: "Diego Ruiz",
    email: "diego.ruiz@empresa.com",
    username: "druiz",
    role: "soporte",
    status: "activo",
    lastLogin: "2026-02-13 08:12",
    team: "Soporte",
    permissions: ["tickets:ver", "tickets:cerrar", "usuarios:ver"],
  },
  {
    id: "USR-1028",
    name: "Elena Suarez",
    email: "elena.suarez@empresa.com",
    username: "esuarez",
    role: "auditor",
    status: "activo",
    lastLogin: "2026-02-11 14:30",
    team: "Compliance",
    permissions: ["logs:ver", "reportes:ver"],
  },
];

const safeParseUsers = (value: string | null): UserRecord[] | null => {
  if (!value) {
    return null;
  }

  try {
    const parsed = JSON.parse(value) as UserRecord[];
    return Array.isArray(parsed) ? parsed : null;
  } catch {
    return null;
  }
};

const loadUsers = (): UserRecord[] => {
  if (typeof window === "undefined") {
    return initialUsers;
  }

  const stored = safeParseUsers(localStorage.getItem(STORAGE_KEY));
  if (stored && stored.length > 0) {
    return stored;
  }

  localStorage.setItem(STORAGE_KEY, JSON.stringify(initialUsers));
  return initialUsers;
};

const saveUsers = (users: UserRecord[]) => {
  if (typeof window === "undefined") {
    return;
  }

  localStorage.setItem(STORAGE_KEY, JSON.stringify(users));
};

export const useUsersStore = () => {
  const [users, setUsers] = useState<UserRecord[]>(initialUsers);

  useEffect(() => {
    setUsers(loadUsers());
  }, []);

  useEffect(() => {
    if (typeof window === "undefined") {
      return undefined;
    }

    const handleStorage = (event: StorageEvent) => {
      if (event.key !== STORAGE_KEY) {
        return;
      }

      const nextUsers = safeParseUsers(event.newValue);
      if (nextUsers) {
        setUsers(nextUsers);
      }
    };

    window.addEventListener("storage", handleStorage);
    return () => window.removeEventListener("storage", handleStorage);
  }, []);

  const updateUsers = useCallback((next: UserRecord[] | ((prev: UserRecord[]) => UserRecord[])) => {
    setUsers((prev) => {
      const resolved = typeof next === "function" ? next(prev) : next;
      saveUsers(resolved);
      return resolved;
    });
  }, []);

  return { users, setUsers: updateUsers };
};
