"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";

interface ServiceRequestDto {
  id: number;
  clientId: string;
  clientName: string;
  professionalId: string;
  professionalName: string;
  serviceId: number;
  serviceTitle: string;
  quotedPrice: number;
  status: number;
  description?: string;
  requestDate: string;
  scheduledDate?: string;
  completionDate?: string;
}

const API = process.env.NEXT_PUBLIC_ALLOWED_PATH;

const STATUS_LABELS: Record<number, string> = {
  0: "Pendiente",
  1: "Aceptada",
  2: "En progreso",
  3: "Completada",
  4: "Cancelada",
};

const STATUS_COLORS: Record<number, { bg: string; border: string; color: string }> = {
  0: { bg: "rgba(251,191,36,0.1)", border: "rgba(251,191,36,0.3)", color: "#fbbf24" },
  1: { bg: "rgba(52,211,153,0.1)", border: "rgba(52,211,153,0.3)", color: "#34d399" },
  2: { bg: "rgba(96,165,250,0.1)", border: "rgba(96,165,250,0.3)", color: "#60a5fa" },
  3: { bg: "rgba(124,58,237,0.1)", border: "rgba(124,58,237,0.3)", color: "#a78bfa" },
  4: { bg: "rgba(248,113,113,0.1)", border: "rgba(248,113,113,0.3)", color: "#f87171" },
};

export default function SolicitudesPage() {
  const router = useRouter();
  const [requests, setRequests] = useState<ServiceRequestDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [userRole, setUserRole] = useState<string>("");
  const [updatingId, setUpdatingId] = useState<number | null>(null);

  useEffect(() => {
    const token = localStorage.getItem("token");
    if (!token) { router.push("/login"); return; }

    const payload = JSON.parse(atob(token.split(".")[1]));
    const role = payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ?? "";
    setUserRole(role);
  }, [router]);

  useEffect(() => {
    if (!userRole) return;
    fetchRequests();
  }, [userRole]);

  const fetchRequests = async () => {
    setLoading(true);
    setError("");
    try {
      const token = localStorage.getItem("token");
      const endpoint = userRole === "Professional"
        ? `${API}/api/servicerequests/professional?page=1&size=50`
        : `${API}/api/servicerequests/my?page=1&size=50`;

      const res = await fetch(endpoint, {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (res.status === 401) { router.push("/login"); return; }
      if (!res.ok) throw new Error();
      const data = await res.json();
      setRequests(data.data ?? data);
    } catch {
      setError("No se pudieron cargar las solicitudes.");
    } finally {
      setLoading(false);
    }
  };

  const updateStatus = async (id: number, newStatus: number) => {
    setUpdatingId(id);
    try {
      const token = localStorage.getItem("token");
      const res = await fetch(
        `${API}/api/servicerequests/${id}/status?newStatus=${newStatus}`,
        {
          method: "PUT",
          headers: { Authorization: `Bearer ${token}` },
        }
      );
      if (!res.ok) throw new Error();
      setRequests((prev) =>
        prev.map((r) => (r.id === id ? { ...r, status: newStatus } : r))
      );
    } catch {
      // silencioso — la card no cambia
    } finally {
      setUpdatingId(null);
    }
  };

  const formatDate = (iso?: string) => {
    if (!iso) return "—";
    return new Date(iso).toLocaleDateString("es-MX", {
      day: "2-digit", month: "short", year: "numeric",
      hour: "2-digit", minute: "2-digit",
    });
  };

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
      {/* Gradientes */}
      <div style={{ position: "absolute", inset: 0, background: "linear-gradient(to bottom, rgba(43,19,95,0.7), #070a0f, #070a0f)" }} />
      <div style={{ position: "absolute", inset: 0, opacity: 0.3, background: "radial-gradient(circle at top, #7c3aed, transparent 60%)" }} />

      <div style={{ position: "relative", maxWidth: "900px", margin: "0 auto" }}>

        {/* Header */}
        <div style={{ marginBottom: "2.5rem", marginTop: "1rem" }}>
          <h1 style={{ color: "white", fontSize: "2rem", fontWeight: 600, margin: 0 }}>
            {userRole === "Professional" ? "Solicitudes recibidas" : "Mis solicitudes"}
          </h1>
          <p style={{ color: "rgba(255,255,255,0.4)", fontSize: "0.9rem", marginTop: "0.4rem" }}>
            {userRole === "Professional"
              ? "Gestiona las solicitudes de tus clientes."
              : "Revisa el estado de tus solicitudes de servicio."}
          </p>
        </div>

        {/* Estados */}
        {loading && (
          <p style={{ color: "rgba(255,255,255,0.4)", textAlign: "center", padding: "4rem 0" }}>
            Cargando solicitudes...
          </p>
        )}

        {error && (
          <p style={{ color: "#f87171", textAlign: "center", padding: "4rem 0" }}>
            {error}
          </p>
        )}

        {!loading && !error && requests.length === 0 && (
          <p style={{ color: "rgba(255,255,255,0.4)", textAlign: "center", padding: "4rem 0" }}>
            No tienes solicitudes aún.
          </p>
        )}

        {/* Lista */}
        {!loading && !error && requests.length > 0 && (
          <div style={{ display: "flex", flexDirection: "column", gap: "1.25rem" }}>
            {requests.map((req) => {
              const statusStyle = STATUS_COLORS[req.status] ?? STATUS_COLORS[0];
              const isUpdating = updatingId === req.id;

              return (
                <div
                  key={req.id}
                  style={{
                    background: "rgba(13,19,27,0.95)",
                    borderRadius: "1.25rem",
                    border: "1px solid rgba(255,255,255,0.08)",
                    padding: "1.5rem",
                    boxShadow: "0 10px 30px rgba(0,0,0,0.4)",
                  }}
                >
                  {/* Fila superior */}
                  <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: "1rem", flexWrap: "wrap", gap: "0.5rem" }}>
                    <div>
                      <h3 style={{ color: "white", fontSize: "1.05rem", fontWeight: 600, margin: 0 }}>
                        {req.serviceTitle}
                      </h3>
                      <p style={{ color: "rgba(255,255,255,0.45)", fontSize: "0.82rem", marginTop: "0.25rem" }}>
                        {userRole === "Professional"
                          ? `Cliente: ${req.clientName}`
                          : `Profesionista: ${req.professionalName}`}
                      </p>
                    </div>

                    {/* Badge status */}
                    <span style={{
                      display: "inline-block",
                      background: statusStyle.bg,
                      border: `1px solid ${statusStyle.border}`,
                      color: statusStyle.color,
                      fontSize: "0.75rem",
                      fontWeight: 600,
                      padding: "0.25rem 0.85rem",
                      borderRadius: "9999px",
                      letterSpacing: "0.04em",
                    }}>
                      {STATUS_LABELS[req.status] ?? "Desconocido"}
                    </span>
                  </div>

                  {/* Descripción */}
                  {req.description && (
                    <p style={{
                      color: "rgba(255,255,255,0.5)",
                      fontSize: "0.875rem",
                      marginBottom: "1rem",
                      lineHeight: 1.6,
                    }}>
                      {req.description}
                    </p>
                  )}

                  {/* Fechas y precio */}
                  <div style={{
                    display: "grid",
                    gridTemplateColumns: "repeat(auto-fill, minmax(180px, 1fr))",
                    gap: "0.75rem",
                    marginBottom: userRole === "Professional" && req.status === 0 ? "1.25rem" : "0",
                  }}>
                    {[
                      { label: "Solicitado", value: formatDate(req.requestDate) },
                      { label: "Fecha programada", value: formatDate(req.scheduledDate) },
                      { label: "Precio cotizado", value: `$${req.quotedPrice.toLocaleString("es-MX", { minimumFractionDigits: 2 })} MXN` },
                    ].map((item) => (
                      <div key={item.label}>
                        <p style={{ color: "rgba(255,255,255,0.3)", fontSize: "0.72rem", textTransform: "uppercase", letterSpacing: "0.05em", margin: 0 }}>
                          {item.label}
                        </p>
                        <p style={{ color: "rgba(255,255,255,0.8)", fontSize: "0.875rem", margin: "0.2rem 0 0" }}>
                          {item.value}
                        </p>
                      </div>
                    ))}
                  </div>

                  {/* Acciones para profesionista — solo en Pendiente (0) */}
                  {userRole === "Professional" && req.status === 0 && (
                    <div style={{ display: "flex", gap: "0.75rem", marginTop: "1.25rem", paddingTop: "1rem", borderTop: "1px solid rgba(255,255,255,0.06)" }}>
                      <button
                        data-testid={`accept-${req.id}`}
                        disabled={isUpdating}
                        onClick={() => updateStatus(req.id, 1)}
                        style={{
                          flex: 1,
                          padding: "0.65rem",
                          borderRadius: "9999px",
                          border: "none",
                          background: isUpdating ? "rgba(52,211,153,0.3)" : "rgba(52,211,153,0.15)",
                          color: "#34d399",
                          fontSize: "0.875rem",
                          fontWeight: 600,
                          cursor: isUpdating ? "not-allowed" : "pointer",
                          fontFamily: "inherit",
                          transition: "background 0.2s",
                        }}
                        onMouseEnter={(e) => { if (!isUpdating) e.currentTarget.style.background = "rgba(52,211,153,0.25)"; }}
                        onMouseLeave={(e) => { if (!isUpdating) e.currentTarget.style.background = "rgba(52,211,153,0.15)"; }}
                      >
                        {isUpdating ? "Procesando..." : "✓ Aceptar"}
                      </button>

                      <button
                        data-testid={`cancel-${req.id}`}
                        disabled={isUpdating}
                        onClick={() => updateStatus(req.id, 4)}
                        style={{
                          flex: 1,
                          padding: "0.65rem",
                          borderRadius: "9999px",
                          border: "none",
                          background: isUpdating ? "rgba(248,113,113,0.3)" : "rgba(248,113,113,0.1)",
                          color: "#f87171",
                          fontSize: "0.875rem",
                          fontWeight: 600,
                          cursor: isUpdating ? "not-allowed" : "pointer",
                          fontFamily: "inherit",
                          transition: "background 0.2s",
                        }}
                        onMouseEnter={(e) => { if (!isUpdating) e.currentTarget.style.background = "rgba(248,113,113,0.2)"; }}
                        onMouseLeave={(e) => { if (!isUpdating) e.currentTarget.style.background = "rgba(248,113,113,0.1)"; }}
                      >
                        {isUpdating ? "Procesando..." : "✕ Cancelar"}
                      </button>
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}
