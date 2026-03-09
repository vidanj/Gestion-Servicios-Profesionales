"use client";

import { useRef, useState } from "react";
import { Text } from "@chakra-ui/react";
import { profileService, type ProfileData } from "@/services/profile.service";

const apiUrl = process.env.NEXT_PUBLIC_ALLOWED_PATH;

type AvatarUploaderProps = {
  profileImageUrl?: string;
  firstName: string;
  onUpdate: (updated: ProfileData) => void;
};

export function AvatarUploader({
  profileImageUrl,
  firstName,
  onUpdate,
}: AvatarUploaderProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const avatarSrc = profileImageUrl ? `${apiUrl}${profileImageUrl}` : null;
  const initials = firstName ? firstName.charAt(0).toUpperCase() : "U";

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setError("");
    setLoading(true);
    try {
      const updated = await profileService.uploadPhoto(file);
      onUpdate(updated);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Error al subir la foto");
    } finally {
      setLoading(false);
      // Limpiar input para permitir re-subir el mismo archivo
      if (inputRef.current) inputRef.current.value = "";
    }
  };

  return (
    <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: "0.75rem" }}>
      <div
        onClick={() => !loading && inputRef.current?.click()}
        style={{
          width: 100,
          height: 100,
          borderRadius: "50%",
          overflow: "hidden",
          cursor: loading ? "default" : "pointer",
          position: "relative",
          border: "2px solid rgba(167,139,250,0.5)",
          background: "rgba(167,139,250,0.15)",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          flexShrink: 0,
        }}
      >
        {avatarSrc ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={avatarSrc}
            alt="Foto de perfil"
            style={{ width: "100%", height: "100%", objectFit: "cover" }}
          />
        ) : (
          <span style={{ fontSize: "2.5rem", color: "#a78bfa", fontWeight: 700 }}>
            {initials}
          </span>
        )}

        {/* Overlay al hacer hover */}
        <div
          style={{
            position: "absolute",
            inset: 0,
            background: "rgba(0,0,0,0.45)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            opacity: loading ? 1 : 0,
            transition: "opacity 0.2s",
            borderRadius: "50%",
          }}
          className="avatar-overlay"
        >
          <span style={{ color: "white", fontSize: "0.7rem", textAlign: "center", padding: "0 8px" }}>
            {loading ? "Subiendo…" : "Cambiar"}
          </span>
        </div>

        <style>{`
          .avatar-overlay { opacity: 0 !important; }
          div:hover > .avatar-overlay { opacity: 1 !important; }
        `}</style>
      </div>

      <Text fontSize="xs" color="gray.400" textAlign="center">
        JPG o PNG · máx. 2 MB
      </Text>

      {error && (
        <Text fontSize="xs" color="red.400" textAlign="center">
          {error}
        </Text>
      )}

      <input
        ref={inputRef}
        type="file"
        accept="image/jpeg,image/png"
        style={{ display: "none" }}
        onChange={handleFileChange}
      />
    </div>
  );
}
