"use client";

import { useEffect, useState } from "react";
import { FiCheck, FiEdit2, FiX } from "react-icons/fi";

type EditableFieldProps = {
  label: string;
  value: string;
  type?: string;
  onSave: (newValue: string) => Promise<void>;
};

export function EditableField({ label, value, type = "text", onSave }: EditableFieldProps) {
  const [editing, setEditing] = useState(false);
  const [current, setCurrent] = useState(value);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  const testId = label
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/\s+/g, "-");

  // Sincronizar si el padre actualiza el valor
  useEffect(() => {
    if (!editing) setCurrent(value);
  }, [value, editing]);

  const hasChanges = current.trim() !== value.trim();

  const handleButton = async () => {
    if (!editing) {
      setEditing(true);
      setError("");
      return;
    }
    if (!hasChanges) {
      setEditing(false);
      setCurrent(value);
      return;
    }
    setSaving(true);
    setError("");
    try {
      await onSave(current.trim());
      setEditing(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Error al guardar");
    } finally {
      setSaving(false);
    }
  };

  const btnColor = editing && hasChanges
    ? { bg: "rgba(52,211,153,0.15)", color: "#34d399" }
    : editing
    ? { bg: "rgba(239,68,68,0.15)", color: "#f87171" }
    : { bg: "rgba(167,139,250,0.15)", color: "#a78bfa" };

  const btnTitle = editing && hasChanges ? "Guardar" : editing ? "Cancelar" : "Editar";

  return (
    <div>
      <p style={{ color: "rgba(255,255,255,0.55)", fontSize: "0.78rem", marginBottom: "0.3rem", letterSpacing: "0.04em", textTransform: "uppercase" }}>
        {label}
      </p>
      <div style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
        <input
          data-testid={`field-${testId}`}
          type={type}
          value={current}
          readOnly={!editing}
          onChange={(e) => setCurrent(e.target.value)}
          style={{
            flex: 1,
            background: editing ? "rgba(167,139,250,0.08)" : "transparent",
            border: `1px solid ${editing ? "rgba(167,139,250,0.45)" : "rgba(255,255,255,0.12)"}`,
            borderRadius: "0.6rem",
            padding: "0.55rem 0.85rem",
            color: "white",
            fontSize: "0.95rem",
            outline: "none",
            cursor: editing ? "text" : "default",
            transition: "border-color 0.2s, background 0.2s",
          }}
        />
        <button
          onClick={handleButton}
          disabled={saving}
          data-testid={`btn-${testId}`}
          title={btnTitle}
          style={{
            width: 34,
            height: 34,
            borderRadius: "50%",
            border: "none",
            background: btnColor.bg,
            color: btnColor.color,
            cursor: saving ? "wait" : "pointer",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            flexShrink: 0,
            transition: "background 0.2s, color 0.2s",
            fontSize: "0.95rem",
          }}
        >
          {saving ? "…" : editing && hasChanges ? <FiCheck /> : editing ? <FiX /> : <FiEdit2 />}
        </button>
      </div>
      {error && (
        <p style={{ color: "#f87171", fontSize: "0.75rem", marginTop: "0.3rem" }}>{error}</p>
      )}
    </div>
  );
}
