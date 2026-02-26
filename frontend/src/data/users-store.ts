"use client";

import { useEffect, useState } from "react";

export type UserRole = "admin" | "manager" | "usuario" | "soporte" | "auditor";

export interface User {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  team?: string;
  permissions?: string[];
}

export type UsersUpdater = User[] | ((prev: User[]) => User[]);

interface UsersStoreSnapshot {
  users: User[];
  setUsers: (value: UsersUpdater) => void;
}

const initialUsers: User[] = [
  {
    id: "u-1001",
    name: "Andrea Pineda",
    email: "andrea.pineda@empresa.com",
    role: "admin",
    team: "Direccion",
    permissions: ["gestion_usuarios", "reportes", "configuracion"],
  },
  {
    id: "u-1002",
    name: "Luis Martinez",
    email: "luis.martinez@empresa.com",
    role: "manager",
    team: "Ventas",
    permissions: ["clientes", "cotizaciones"],
  },
  {
    id: "u-1003",
    name: "Camila Sosa",
    email: "camila.sosa@empresa.com",
    role: "usuario",
    team: "Operaciones",
    permissions: ["ordenes", "inventario"],
  },
  {
    id: "u-1004",
    name: "Jorge Padilla",
    email: "jorge.padilla@empresa.com",
    role: "soporte",
    team: "Soporte",
    permissions: ["tickets", "base_conocimiento"],
  },
  {
    id: "u-1005",
    name: "Paula Reyes",
    email: "paula.reyes@empresa.com",
    role: "auditor",
    team: "Auditoria",
    permissions: ["logs", "cumplimiento"],
  },
];

let usersState = initialUsers;
const listeners = new Set<(users: User[]) => void>();

const notify = () => {
  listeners.forEach((listener) => listener(usersState));
};

const setUsers = (value: UsersUpdater) => {
  usersState = typeof value === "function" ? value(usersState) : value;
  notify();
};

export const useUsersStore = (): UsersStoreSnapshot => {
  const [users, setUsersState] = useState(usersState);

  useEffect(() => {
    const listener = (nextUsers: User[]) => setUsersState(nextUsers);
    listeners.add(listener);
    return () => {
      listeners.delete(listener);
    };
  }, []);

  return { users, setUsers };
};
