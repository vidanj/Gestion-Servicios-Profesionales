"use client";

import Link from "next/link";

export default function CatalogoPage() {
  const cards = [
    { name: "Carlos M.", service: "Creo canciones personalizadas", rating: "4.8" },
    { name: "Andrea L.", service: "Edito videos profesionales", rating: "4.5" },
    { name: "Luis R.", service: "Hago páginas web modernas", rating: "5" },
    { name: "Mariana G.", service: "Diseño logos creativos", rating: "4.7" },
    { name: "Fernando P.", service: "Producción musical completa", rating: "4.6" },
    { name: "Sofía T.", service: "Traducciones profesionales", rating: "4.9" },
  ];

  return (
    <div
      style={{
        minHeight: "100vh",
        background: "#070a0f",
        padding: "3rem 2rem",
        position: "relative",
        overflow: "hidden",
      }}
    >
      {/* Background Gradients */}
      <div
        style={{
          position: "absolute",
          inset: 0,
          background:
            "linear-gradient(to bottom, rgba(43,19,95,0.7), #070a0f, #070a0f)",
        }}
      />
      <div
        style={{
          position: "absolute",
          inset: 0,
          opacity: 0.3,
          background: "radial-gradient(circle at top, #7c3aed, transparent 60%)",
        }}
      />

      <div style={{ position: "relative", maxWidth: "1200px", margin: "0 auto" }}>
        
        {/* Header */}
        <div style={{ marginBottom: "3rem" }}>
          <h1
            style={{
              color: "white",
              fontSize: "2rem",
              marginBottom: "1.5rem",
              fontWeight: 600,
            }}
          >
            Catálogo de Servicios
          </h1>

          {/* Search bar (solo vista) */}
          <input
            type="text"
            placeholder="Buscar servicios..."
            style={{
              width: "100%",
              padding: "1rem 1.5rem",
              borderRadius: "9999px",
              border: "1px solid rgba(255,255,255,0.1)",
              background: "#05070b",
              color: "white",
              fontSize: "1rem",
              outline: "none",
            }}
          />
        </div>

        {/* Grid */}
        <div
          style={{
            display: "grid",
            gridTemplateColumns: "repeat(auto-fill, minmax(280px, 1fr))",
            gap: "2rem",
          }}
        >
          {cards.map((card, index) => (
            <div
              key={index}
              style={{
                background: "rgba(13,19,27,0.95)",
                borderRadius: "1.5rem",
                border: "1px solid rgba(255,255,255,0.08)",
                overflow: "hidden",
                boxShadow: "0 20px 40px rgba(0,0,0,0.6)",
                transition: "0.3s ease",
              }}
            >
              {/* Imagen / placeholder */}
              <div
                style={{
                  height: "160px",
                  background: "#000",
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "center",
                  color: "rgba(255,255,255,0.3)",
                  fontSize: "0.9rem",
                }}
              >
                Sin foto
              </div>

              {/* Contenido */}
              <div style={{ padding: "1.5rem" }}>
                <h3
                  style={{
                    color: "white",
                    fontSize: "1.1rem",
                    marginBottom: "0.5rem",
                  }}
                >
                  {card.service}
                </h3>

                <p
                  style={{
                    color: "rgba(255,255,255,0.6)",
                    fontSize: "0.9rem",
                    marginBottom: "0.8rem",
                  }}
                >
                  {card.name}
                </p>

                {/* Rating */}
                <div style={{ display: "flex", alignItems: "center", gap: "0.4rem" }}>
                  <span style={{ color: "#facc15", fontSize: "1rem" }}>★</span>
                  <span style={{ color: "white", fontSize: "0.9rem" }}>
                    {card.rating}
                  </span>
                </div>
              </div>
            </div>
          ))}
        </div>

        {/* Back button */}
        <div style={{ marginTop: "3rem", textAlign: "center" }}>
          <Link
            href="/login"
            style={{
              color: "rgba(255,255,255,0.7)",
              textDecoration: "none",
              fontSize: "0.9rem",
            }}
          >
            Cerrar sesión
          </Link>
        </div>
      </div>
    </div>
  );
}