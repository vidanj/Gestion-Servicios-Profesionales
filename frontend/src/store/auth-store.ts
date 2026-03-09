import { create } from "zustand";
import { persist } from "zustand/middleware";

type AuthUser = {
  firstName: string;
  lastName: string;
  email: string;
  role: string;
  profileImageUrl?: string;
};

type AuthStore = AuthUser & {
  setFromAuthResponse: (data: { firstName: string; lastName: string; email: string; role: string }) => void;
  setFromProfile: (data: { firstName: string; lastName: string; email: string; role: number; profileImageUrl?: string }) => void;
  clear: () => void;
};

const empty: AuthUser = {
  firstName: "",
  lastName: "",
  email: "",
  role: "",
  profileImageUrl: undefined,
};

export const useAuthStore = create<AuthStore>()(
  persist(
    (set) => ({
      ...empty,

      setFromAuthResponse: (data) =>
        set({
          firstName: data.firstName,
          lastName: data.lastName,
          email: data.email,
          role: data.role,
        }),

      setFromProfile: (data) =>
        set({
          firstName: data.firstName,
          lastName: data.lastName,
          email: data.email,
          role: String(data.role),
          profileImageUrl: data.profileImageUrl,
        }),

      clear: () => set({ ...empty }),
    }),
    { name: "auth-user" }
  )
);
