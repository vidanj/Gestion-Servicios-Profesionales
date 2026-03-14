"use client";

import { useEffect, useState } from "react";

export type UserRole = "admin" | "cliente" | "profesionista";

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
  loading: boolean;
  error: string;
  refresh: () => Promise<void>;
}

const roleMap: Record<number, UserRole> = {
  0: "admin",
  1: "cliente",
  2: "profesionista",
};

const apiUrl = process.env.NEXT_PUBLIC_ALLOWED_PATH;

const getToken = () =>
  typeof window !== "undefined" ? localStorage.getItem("token") : null;

// Estado global compartido entre componentes
let usersState: User[] = [];
let loadingState = false;
let errorState = "";
const listeners = new Set<() => void>();

const notify = () => listeners.forEach((l) => l());

const setUsers = (value: UsersUpdater) => {
  usersState = typeof value === "function" ? value(usersState) : value;
  notify();
};

const fetchFromApi = async () => {
  const token = getToken();
  if (!token) return;

  loadingState = true;
  errorState = "";
  notify();

  try {
    const res = await fetch(`${apiUrl}/api/Users?page=1&size=50`, {
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
      },
    });

    if (!res.ok) throw new Error("Error al cargar usuarios");

    const json = await res.json();
    const raw: { id: string; firstName: string; lastName: string; email: string; role: number }[] =
      json.data ?? json.Data ?? [];

    usersState = raw.map((u) => ({
      id: u.id,
      name: `${u.firstName} ${u.lastName}`,
      email: u.email,
      role: roleMap[u.role] ?? "cliente",
    }));
  } catch {
    errorState = "No se pudieron cargar los usuarios.";
  } finally {
    loadingState = false;
    notify();
  }
};

export const useUsersStore = (): UsersStoreSnapshot => {
  const [, forceUpdate] = useState(0);

  useEffect(() => {
    const listener = () => forceUpdate((n) => n + 1);
    listeners.add(listener);

    if (usersState.length === 0 && !loadingState) {
      fetchFromApi();
    }

    return () => {
      listeners.delete(listener);
    };
  }, []);

  return {
    users: usersState,
    setUsers,
    loading: loadingState,
    error: errorState,
    refresh: fetchFromApi,
  };
};