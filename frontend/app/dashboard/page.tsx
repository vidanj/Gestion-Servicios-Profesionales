"use client";

import Link from "next/link";
import { useEffect, useState } from "react";

interface ServiceDto {
  id: number;
  professionalId: string;
  professionalName: string;
  categoryId: number;
  categoryName: string;
  title: string;
  description?: string;
  basePrice: number;
  imageUrl?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

const API = process.env.NEXT_PUBLIC_ALLOWED_PATH;

export default function CatalogoPage() {
  const [services, setServices] = useState<ServiceDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [allServices, setAllServices] = useState<ServiceDto[]>([]);
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(search);
    }, 800);
    return () => clearTimeout(timer);
  }, [search]);

  useEffect(() => {
    const fetchServices = async () => {
      try {
        const token = localStorage.getItem("token");
        const res = await fetch(`${API}/api/services?page=1&size=50`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        if (!res.ok) throw new Error();
        const data = await res.json();
        const list = data.data ?? data;
        setAllServices(list);
        setServices(list);
      } catch {
        setError("No se pudieron cargar los servicios.");
      } finally {
        setLoading(false);
      }
    };
    fetchServices();
  }, []);

  useEffect(() => {
    if (!debouncedSearch.trim()) {
      setServices(allServices);
      return;
    }
    const q = debouncedSearch.toLowerCase();
    setServices(
      allServices.filter(
        (s) =>
          s.title.toLowerCase().includes(q) ||
          s.professionalName.toLowerCase().includes(q) ||
          s.categoryName.toLowerCase().includes(q) ||
          s.description?.toLowerCase().includes(q)
      )
    );
  }, [debouncedSearch, allServices]);

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
        <div style={{ marginBottom: "3rem", marginTop: "3rem" }}>
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

          {/* Search bar */}
          <input
            type="text"
            placeholder="Buscar servicios..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
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

        {/* Estados */}
        {loading && (
          <p style={{ color: "rgba(255,255,255,0.4)", textAlign: "center", padding: "4rem 0" }}>
            Cargando servicios...
          </p>
        )}

        {error && (
          <p style={{ color: "#f87171", textAlign: "center", padding: "4rem 0" }}>
            {error}
          </p>
        )}

        {!loading && !error && services.length === 0 && (
          <p style={{ color: "rgba(255,255,255,0.4)", textAlign: "center", padding: "4rem 0" }}>
            No hay servicios disponibles.
          </p>
        )}

        {/* Grid */}
        {!loading && !error && services.length > 0 && (
          <div
            style={{
              display: "grid",
              gridTemplateColumns: "repeat(auto-fill, minmax(280px, 1fr))",
              gap: "2rem",
            }}
          >
            {services.map((service) => (
              <Link
                key={service.id}
                href={`/catalogo/${service.id}`}
                style={{ textDecoration: "none" }}
              >
                <div
                  style={{
                    background: "rgba(13,19,27,0.95)",
                    borderRadius: "1.5rem",
                    border: "1px solid rgba(255,255,255,0.08)",
                    overflow: "hidden",
                    boxShadow: "0 20px 40px rgba(0,0,0,0.6)",
                    transition: "transform 0.2s ease, border-color 0.2s ease",
                    cursor: "pointer",
                  }}
                  onMouseEnter={(e) => {
                    (e.currentTarget as HTMLDivElement).style.transform = "translateY(-4px)";
                    (e.currentTarget as HTMLDivElement).style.borderColor = "rgba(124,58,237,0.5)";
                  }}
                  onMouseLeave={(e) => {
                    (e.currentTarget as HTMLDivElement).style.transform = "translateY(0)";
                    (e.currentTarget as HTMLDivElement).style.borderColor = "rgba(255,255,255,0.08)";
                  }}
                >
                {/* Imagen */}
                <div
                  style={{
                    height: "160px",
                    background: "#000",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    color: "rgba(255,255,255,0.3)",
                    fontSize: "0.9rem",
                    overflow: "hidden",
                  }}
                >
                  {service.imageUrl ? (
                    <img
                      src={service.imageUrl}
                      alt={service.title}
                      style={{ width: "100%", height: "100%", objectFit: "cover" }}
                      onError={(e) => {
                        e.currentTarget.style.display = "none";
                        e.currentTarget.parentElement!.innerText = "Sin foto";
                      }}
                    />
                  ) : (
                    "Sin foto"
                  )}
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
                    {service.title}
                  </h3>

                  {service.description && (
                    <p
                      style={{
                        color: "rgba(255,255,255,0.45)",
                        fontSize: "0.85rem",
                        marginBottom: "0.5rem",
                        display: "-webkit-box",
                        WebkitLineClamp: 2,
                        WebkitBoxOrient: "vertical",
                        overflow: "hidden",
                      }}
                    >
                      {service.description}
                    </p>
                  )}

                  <p
                    style={{
                      color: "rgba(255,255,255,0.6)",
                      fontSize: "0.9rem",
                      marginBottom: "0.8rem",
                    }}
                  >
                    {service.professionalName}
                  </p>

                  <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                    {/* Rating ficticio */}
                    <div style={{ display: "flex", alignItems: "center", gap: "0.4rem" }}>
                      <span style={{ color: "#facc15", fontSize: "1rem" }}>★</span>
                      <span style={{ color: "white", fontSize: "0.9rem" }}>4.8</span>
                    </div>

                    {/* Precio */}
                    <span style={{ color: "#a78bfa", fontWeight: 600, fontSize: "0.95rem" }}>
                      ${service.basePrice.toLocaleString("es-MX", { minimumFractionDigits: 2 })} MXN
                    </span>
                  </div>
                </div>
              </div>
              </Link>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
