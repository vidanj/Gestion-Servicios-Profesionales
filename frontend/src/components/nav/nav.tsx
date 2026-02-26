"use client";

import Link from "next/link";

export default function Nav() {
  return (
    <nav
      style={{
        width: "100%",
        padding: "1rem 2rem",
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

      <div style={{ display: "flex", gap: "1.5rem" }}>
        <Link href="/dashboard" style={{ color: "rgba(255,255,255,0.7)", textDecoration: "none" }}>
          Catálogos
        </Link>
        <Link href="/login" style={{ color: "rgba(255,255,255,0.7)", textDecoration: "none" }}>
          Logout
        </Link>
      </div>
    </nav>
  );
}