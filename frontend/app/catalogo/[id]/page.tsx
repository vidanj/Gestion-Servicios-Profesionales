"use client";

import { useEffect, useState } from "react";
import { useRouter, useParams } from "next/navigation";
import Link from "next/link";

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
}

interface FormState {
  description: string;
  scheduledDate: string;
}

interface FormErrors {
  description?: string;
  scheduledDate?: string;
}

const API = process.env.NEXT_PUBLIC_ALLOWED_PATH;

const inputStyle: React.CSSProperties = {
  display: "block",
  width: "100%",
  boxSizing: "border-box",
  padding: "1.1rem 1.5rem",
  borderRadius: "9999px",
  background: "#05070b",
  border: "1px solid rgba(255,255,255,0.1)",
  color: "white",
  fontSize: "1rem",
  outline: "none",
  fontFamily: "inherit",
};

const labelStyle: React.CSSProperties = {
  color: "rgba(255,255,255,0.55)",
  fontSize: "0.78rem",
  fontWeight: 500,
  marginBottom: "0.4rem",
  paddingLeft: "1.2rem",
  display: "block",
  letterSpacing: "0.05em",
  textTransform: "uppercase",
};

const errorStyle: React.CSSProperties = {
  color: "#f87171",
  fontSize: "0.78rem",
  marginTop: "0.35rem",
  paddingLeft: "1.2rem",
};

export default function ServiceDetailPage() {
  const router = useRouter();
  const params = useParams();
  const id = params?.id as string;

  const [service, setService] = useState<ServiceDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [form, setForm] = useState<FormState>({ description: "", scheduledDate: "" });
  const [formErrors, setFormErrors] = useState<FormErrors>({});
  const [submitting, setSubmitting] = useState(false);
  const [success, setSuccess] = useState(false);
  const [serverError, setServerError] = useState("");

  useEffect(() => {
    const token = localStorage.getItem("token");
    if (token) {
      const payload = JSON.parse(atob(token.split(".")[1]));
      console.log("JWT payload:", payload);
    }
  }, []);

  useEffect(() => {
    const fetchService = async () => {
      try {
        const token = localStorage.getItem("token");
        const res = await fetch(`${API}/api/services/${id}`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        if (res.status === 401) { router.push("/login"); return; }
        if (!res.ok) throw new Error();
        const data: ServiceDto = await res.json();
        setService(data);
      } catch {
        setError("No se pudo cargar el servicio.");
      } finally {
        setLoading(false);
      }
    };
    if (id) fetchService();
  }, [id, router]);

  const validate = (): boolean => {
    const newErrors: FormErrors = {};

    if (!form.scheduledDate) {
      newErrors.scheduledDate = "La fecha es requerida.";
    } else {
      const selected = new Date(form.scheduledDate);
      const now = new Date();
      if (selected <= now) {
        newErrors.scheduledDate = "La fecha debe ser futura.";
      }
    }

    if (form.description.length > 500) {
      newErrors.description = "La descripción no puede superar los 500 caracteres.";
    }

    setFormErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    setForm((prev) => ({ ...prev, [name]: value }));
    if (formErrors[name as keyof FormErrors]) {
      setFormErrors((prev) => ({ ...prev, [name]: undefined }));
    }
  };

  const handleSubmit = async () => {
    setServerError("");
    if (!validate()) return;

    setSubmitting(true);
    try {
      const token = localStorage.getItem("token");
      const payload = {
        serviceId: service!.id,
        description: form.description.trim() || undefined,
        scheduledDate: new Date(form.scheduledDate).toISOString(),
      };
      console.log("Payload enviado:", JSON.stringify(payload, null, 2));
      const res = await fetch(`${API}/api/servicerequests`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify(payload),
      });

      if (res.status === 401) { router.push("/login"); return; }
      if (res.status === 403) {
        setServerError("Solo los clientes pueden solicitar servicios.");
        return;
      }
    if (!res.ok) {
      const data = await res.json().catch(() => ({}));
      console.log("Error 400 response:", data);
      setServerError(data?.message ?? "Ocurrió un error al enviar la solicitud.");
      return;
    }

      setSuccess(true);
    } catch {
      setServerError("No se pudo conectar con el servidor.");
    } finally {
      setSubmitting(false);
    }
  };

  // Fecha mínima: ahora + 1 hora
  const minDate = new Date(Date.now() + 60 * 60 * 1000)
    .toISOString()
    .slice(0, 16);

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

      <div style={{ position: "relative", maxWidth: "800px", margin: "0 auto" }}>

        {/* Volver */}
        <Link
          href="/dashboard"
          style={{
            display: "inline-flex",
            alignItems: "center",
            gap: "0.5rem",
            color: "rgba(255,255,255,0.5)",
            textDecoration: "none",
            fontSize: "0.875rem",
            marginBottom: "2rem",
          }}
        >
          ← Volver al catálogo
        </Link>

        {loading && (
          <p style={{ color: "rgba(255,255,255,0.4)", textAlign: "center", padding: "4rem 0" }}>
            Cargando...
          </p>
        )}

        {error && (
          <p style={{ color: "#f87171", textAlign: "center", padding: "4rem 0" }}>
            {error}
          </p>
        )}

        {service && (
          <div style={{ display: "flex", flexDirection: "column", gap: "2rem" }}>

            {/* Card de detalle del servicio */}
            <div
              style={{
                background: "rgba(13,19,27,0.95)",
                borderRadius: "1.5rem",
                border: "1px solid rgba(255,255,255,0.08)",
                overflow: "hidden",
                boxShadow: "0 20px 40px rgba(0,0,0,0.6)",
              }}
            >
              {/* Imagen */}
              {service.imageUrl ? (
                <img
                  src={service.imageUrl}
                  alt={service.title}
                  style={{ width: "100%", height: "260px", objectFit: "cover" }}
                  onError={(e) => { e.currentTarget.style.display = "none"; }}
                />
              ) : (
                <div style={{
                  height: "180px",
                  background: "linear-gradient(to bottom, rgba(109,40,217,0.2), transparent)",
                  display: "flex", alignItems: "center", justifyContent: "center",
                  color: "rgba(255,255,255,0.2)", fontSize: "0.9rem",
                }}>
                  Sin imagen
                </div>
              )}

              <div style={{ padding: "2rem" }}>
                {/* Categoría badge */}
                <span style={{
                  display: "inline-block",
                  background: "rgba(124,58,237,0.2)",
                  border: "1px solid rgba(124,58,237,0.4)",
                  color: "#a78bfa",
                  fontSize: "0.75rem",
                  fontWeight: 600,
                  padding: "0.25rem 0.75rem",
                  borderRadius: "9999px",
                  marginBottom: "1rem",
                  letterSpacing: "0.04em",
                  textTransform: "uppercase",
                }}>
                  {service.categoryName}
                </span>

                <h1 style={{ color: "white", fontSize: "1.6rem", fontWeight: 700, margin: "0 0 0.5rem" }}>
                  {service.title}
                </h1>

                <p style={{ color: "rgba(255,255,255,0.55)", fontSize: "0.9rem", marginBottom: "1.5rem" }}>
                  Por <strong style={{ color: "#c4b5fd" }}>{service.professionalName}</strong>
                </p>

                {service.description && (
                  <p style={{
                    color: "rgba(255,255,255,0.65)",
                    fontSize: "0.95rem",
                    lineHeight: 1.7,
                    marginBottom: "1.5rem",
                  }}>
                    {service.description}
                  </p>
                )}

                <div style={{
                  display: "flex",
                  alignItems: "center",
                  justifyContent: "space-between",
                  paddingTop: "1rem",
                  borderTop: "1px solid rgba(255,255,255,0.08)",
                }}>
                  <div style={{ display: "flex", alignItems: "center", gap: "0.4rem" }}>
                    <span style={{ color: "#facc15" }}>★</span>
                    <span style={{ color: "white", fontSize: "0.9rem" }}>4.8</span>
                  </div>
                  <span style={{ color: "#a78bfa", fontWeight: 700, fontSize: "1.2rem" }}>
                    ${service.basePrice.toLocaleString("es-MX", { minimumFractionDigits: 2 })} MXN
                  </span>
                </div>
              </div>
            </div>

            {/* Formulario de solicitud */}
            <div
              style={{
                background: "rgba(13,19,27,0.95)",
                borderRadius: "1.5rem",
                border: "1px solid rgba(255,255,255,0.08)",
                padding: "2rem",
                boxShadow: "0 20px 40px rgba(0,0,0,0.6)",
              }}
            >
              {success ? (
                <div
                  data-testid="success-message"
                  style={{ textAlign: "center", padding: "1.5rem 0", display: "flex", flexDirection: "column", alignItems: "center", gap: "1rem" }}
                >
                  <div style={{
                    width: "4rem", height: "4rem", borderRadius: "9999px",
                    background: "rgba(52,211,153,0.15)", border: "1px solid rgba(52,211,153,0.4)",
                    display: "flex", alignItems: "center", justifyContent: "center",
                  }}>
                    <svg xmlns="http://www.w3.org/2000/svg" width="28" height="28" viewBox="0 0 24 24"
                      fill="none" stroke="#34d399" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                      <polyline points="20 6 9 17 4 12" />
                    </svg>
                  </div>
                  <p style={{ color: "#34d399", fontSize: "1rem", fontWeight: 600 }}>
                    ¡Solicitud enviada exitosamente!
                  </p>
                  <p style={{ color: "rgba(255,255,255,0.4)", fontSize: "0.875rem" }}>
                    El profesionista revisará tu solicitud pronto.
                  </p>
                  <Link href="/dashboard" style={{
                    marginTop: "0.5rem", padding: "0.75rem 2rem", borderRadius: "9999px",
                    border: "1px solid rgba(255,255,255,0.15)", background: "transparent",
                    color: "rgba(255,255,255,0.7)", fontSize: "0.875rem", textDecoration: "none",
                  }}>
                    Volver al catálogo
                  </Link>
                </div>
              ) : (
                <>
                  <h2 style={{ color: "white", fontSize: "1.1rem", fontWeight: 600, marginBottom: "1.5rem" }}>
                    Solicitar este servicio
                  </h2>

                  <div style={{ display: "flex", flexDirection: "column", gap: "1.25rem" }}>

                    {/* Fecha */}
                    <div>
                      <label style={labelStyle}>Fecha preferida</label>
                      <input
                        data-testid="date-input"
                        type="datetime-local"
                        name="scheduledDate"
                        value={form.scheduledDate}
                        onChange={handleChange}
                        min={minDate}
                        style={{
                          ...inputStyle,
                          colorScheme: "dark",
                          borderColor: formErrors.scheduledDate ? "#f87171" : "rgba(255,255,255,0.1)",
                        }}
                        onFocus={(e) => {
                          e.currentTarget.style.boxShadow = "0 0 0 2px #7c3aed";
                          e.currentTarget.style.borderColor = "#7c3aed";
                        }}
                        onBlur={(e) => {
                          e.currentTarget.style.boxShadow = "none";
                          e.currentTarget.style.borderColor = formErrors.scheduledDate ? "#f87171" : "rgba(255,255,255,0.1)";
                        }}
                      />
                      {formErrors.scheduledDate && (
                        <p data-testid="error-date" style={errorStyle}>{formErrors.scheduledDate}</p>
                      )}
                    </div>

                    {/* Descripción */}
                    <div>
                      <label style={labelStyle}>Descripción (opcional)</label>
                      <textarea
                        data-testid="description-input"
                        name="description"
                        value={form.description}
                        onChange={handleChange}
                        placeholder="Describe lo que necesitas con más detalle..."
                        maxLength={500}
                        rows={4}
                        style={{
                          ...inputStyle,
                          borderRadius: "1.25rem",
                          resize: "none",
                          borderColor: formErrors.description ? "#f87171" : "rgba(255,255,255,0.1)",
                        }}
                        onFocus={(e) => {
                          e.currentTarget.style.boxShadow = "0 0 0 2px #7c3aed";
                          e.currentTarget.style.borderColor = "#7c3aed";
                        }}
                        onBlur={(e) => {
                          e.currentTarget.style.boxShadow = "none";
                          e.currentTarget.style.borderColor = formErrors.description ? "#f87171" : "rgba(255,255,255,0.1)";
                        }}
                      />
                      <div style={{ display: "flex", justifyContent: "space-between", marginTop: "0.35rem", paddingLeft: "1.2rem" }}>
                        {formErrors.description ? (
                          <p data-testid="error-description" style={{ ...errorStyle, margin: 0, padding: 0 }}>{formErrors.description}</p>
                        ) : <span />}
                        <span style={{ color: "rgba(255,255,255,0.25)", fontSize: "0.72rem" }}>
                          {form.description.length}/500
                        </span>
                      </div>
                    </div>

                    {serverError && (
                      <p data-testid="server-error" style={{ ...errorStyle, textAlign: "center", paddingLeft: 0, fontSize: "0.875rem" }}>
                        {serverError}
                      </p>
                    )}

                    <button
                      data-testid="submit-button"
                      type="button"
                      onClick={handleSubmit}
                      disabled={submitting}
                      style={{
                        display: "block", width: "100%", boxSizing: "border-box",
                        padding: "1rem", borderRadius: "9999px", border: "none",
                        background: submitting ? "rgba(124,58,237,0.5)" : "linear-gradient(to right, #6d28d9, #7c3aed)",
                        color: "white", fontSize: "1rem", fontWeight: 600,
                        cursor: submitting ? "not-allowed" : "pointer",
                        boxShadow: "0 10px 15px -3px rgba(124,58,237,0.3)",
                        fontFamily: "inherit", letterSpacing: "0.05em",
                      }}
                      onMouseEnter={(e) => { if (!submitting) e.currentTarget.style.background = "linear-gradient(to right, #5b21b6, #6d28d9)"; }}
                      onMouseLeave={(e) => { if (!submitting) e.currentTarget.style.background = "linear-gradient(to right, #6d28d9, #7c3aed)"; }}
                    >
                      {submitting ? "ENVIANDO..." : "ENVIAR SOLICITUD"}
                    </button>
                  </div>
                </>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
