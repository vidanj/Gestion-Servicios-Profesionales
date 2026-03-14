"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useAuthStore } from "@/store/auth-store";

const apiUrl = process.env.NEXT_PUBLIC_ALLOWED_PATH;

export default function Nav() {
  const router = useRouter();
  const [mounted, setMounted] = useState(false);
  const { firstName, profileImageUrl, clear } = useAuthStore();

  useEffect(() => setMounted(true), []);

  const handleLogout = () => {
    localStorage.removeItem("token");
    clear();
    router.push("/login");
  };

  const initial = firstName ? firstName.charAt(0).toUpperCase() : "U";
  const avatarSrc = profileImageUrl ? `${apiUrl}${profileImageUrl}` : null;

  return (
    <nav
      style={{
        width: "100%",
        padding: "0.75rem 2rem",
        background: "rgba(13,19,27,0.9)",
        borderBottom: "1px solid rgba(255,255,255,0.08)",
        display: "flex",
        justifyContent: "space-between",
        alignItems: "center",
        position: "sticky",
        top: 0,
        zIndex: 100,
      }}
    >
      <Link href="/dashboard" style={{ color: "white", textDecoration: "none", fontWeight: 600 }}>
        MiFiverr
      </Link>

      <div style={{ display: "flex", gap: "1.5rem", alignItems: "center" }}>
        <Link href="/profesionista" style={{ color: "rgba(255,255,255,0.7)", textDecoration: "none" }}>
          Mis Servicios
        </Link>
        <Link href="/solicitudes" style={{ color: "rgba(255,255,255,0.7)", textDecoration: "none" }}>
          Solicitudes
        </Link>

        <Link href="/usuarios" style={{ color: "rgba(255,255,255,0.7)", textDecoration: "none" }}>
          Administración
        </Link>

        <Link href="/dashboard" style={{ color: "rgba(255,255,255,0.7)", textDecoration: "none" }}>
          Catálogos
        </Link>

        <Link href="/about" style={{ color: "rgba(255,255,255,0.7)", textDecoration: "none" }}>
          Acerca de
        </Link>

        {/* Avatar → Mi Perfil */}
        <Link
          href="/perfil"
          title="Mi Perfil"
          style={{ display: "flex", alignItems: "center", gap: "0.5rem", textDecoration: "none" }}
        >
          <div
            style={{
              width: 32,
              height: 32,
              borderRadius: "50%",
              overflow: "hidden",
              border: "2px solid rgba(167,139,250,0.5)",
              background: "rgba(167,139,250,0.15)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              flexShrink: 0,
            }}
          >
            {mounted && avatarSrc ? (
              // eslint-disable-next-line @next/next/no-img-element
              <img src={avatarSrc} alt="avatar" style={{ width: "100%", height: "100%", objectFit: "cover" }} />
            ) : (
              <span style={{ color: "#a78bfa", fontSize: "0.8rem", fontWeight: 700 }}>
                {mounted ? initial : "U"}
              </span>
            )}
          </div>
          <span style={{ color: "rgba(255,255,255,0.7)", fontSize: "0.9rem" }}>
            {mounted && firstName ? firstName : "Mi Perfil"}
          </span>
        </Link>

        <button
          onClick={handleLogout}
          style={{
            background: "none",
            border: "none",
            color: "rgba(255,255,255,0.7)",
            cursor: "pointer",
            fontSize: "inherit",
            fontFamily: "inherit",
            padding: 0,
          }}
        >
          Logout
        </button>
      </div>
    </nav>
  );
}
