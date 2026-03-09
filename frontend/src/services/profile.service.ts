const apiUrl = process.env.NEXT_PUBLIC_ALLOWED_PATH;

const getToken = () =>
  typeof window !== "undefined" ? localStorage.getItem("token") : null;

const authHeaders = () => ({
  "Content-Type": "application/json",
  Authorization: `Bearer ${getToken()}`,
});

export type ProfileData = {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  role: number;
  profileImageUrl?: string;
  averageRating: number;
  status: boolean;
  createdAt: string;
};

export type UpdateProfilePayload = {
  firstName: string;
  lastName: string;
  phoneNumber?: string;
};

export type ChangePasswordPayload = {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
};

export const profileService = {
  async getProfile(): Promise<ProfileData> {
    const res = await fetch(`${apiUrl}/api/Profile`, {
      headers: authHeaders(),
    });
    if (!res.ok) throw new Error("No se pudo cargar el perfil");
    return res.json();
  },

  async updateProfile(data: UpdateProfilePayload): Promise<ProfileData> {
    const res = await fetch(`${apiUrl}/api/Profile`, {
      method: "PUT",
      headers: authHeaders(),
      body: JSON.stringify(data),
    });
    if (!res.ok) {
      const err = await res.json();
      throw new Error(err.message ?? "Error al actualizar el perfil");
    }
    return res.json();
  },

  async changePassword(data: ChangePasswordPayload): Promise<void> {
    const res = await fetch(`${apiUrl}/api/Profile/password`, {
      method: "PUT",
      headers: authHeaders(),
      body: JSON.stringify(data),
    });
    if (!res.ok) {
      const err = await res.json();
      throw new Error(err.message ?? "Error al cambiar la contraseña");
    }
  },

  async uploadPhoto(file: File): Promise<ProfileData> {
    const formData = new FormData();
    formData.append("foto", file);
    const res = await fetch(`${apiUrl}/api/Profile/foto`, {
      method: "POST",
      // Sin Content-Type: el navegador lo establece con el boundary de multipart
      headers: { Authorization: `Bearer ${getToken()}` },
      body: formData,
    });
    if (!res.ok) {
      const err = await res.json();
      throw new Error(err.message ?? "Error al subir la foto");
    }
    return res.json();
  },
};
