"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";

interface CategoryDto {
    id: number;
    name: string;
    description?: string;
    iconUrl?: string;
    isActive: boolean;
}

interface FormState {
    categoryId: string;
    title: string;
    description: string;
    basePrice: string;
    imageUrl: string;
}

interface FormErrors {
    categoryId?: string;
    title?: string;
    description?: string;
    basePrice?: string;
    imageUrl?: string;
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

const errorStyle: React.CSSProperties = {
    color: "#f87171",
    fontSize: "0.78rem",
    marginTop: "0.35rem",
    paddingLeft: "1.2rem",
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

function ImagePreview({ url }: { url: string }) {
    const [status, setStatus] = useState<"loading" | "ok" | "error">("loading");

    useEffect(() => {
        setStatus("loading");
        const img = new Image();
        img.onload = () => setStatus("ok");
        img.onerror = () => setStatus("error");
        img.src = url;
    }, [url]);

    return (
        <div
            style={{
                marginTop: "0.75rem",
                borderRadius: "1rem",
                overflow: "hidden",
                border: "1px solid rgba(255,255,255,0.08)",
                height: "160px",
                background: "#05070b",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
            }}
        >
            {status === "loading" && (
                <span
                    style={{
                        color: "rgba(255,255,255,0.3)",
                        fontSize: "0.85rem",
                    }}
                >
                    Cargando preview...
                </span>
            )}
            {status === "error" && (
                <span style={{ color: "#f87171", fontSize: "0.85rem" }}>
                    No se pudo cargar la imagen
                </span>
            )}
            {status === "ok" && (
                <img
                    src={url}
                    alt="Preview"
                    style={{
                        width: "100%",
                        height: "100%",
                        objectFit: "cover",
                    }}
                />
            )}
        </div>
    );
}

export default function PublicarServicioPage() {
    const router = useRouter();

    const [form, setForm] = useState<FormState>({
        categoryId: "",
        title: "",
        description: "",
        basePrice: "",
        imageUrl: "",
    });

    const [errors, setErrors] = useState<FormErrors>({});
    const [categories, setCategories] = useState<CategoryDto[]>([]);
    const [loadingCats, setLoadingCats] = useState(true);
    const [submitting, setSubmitting] = useState(false);
    const [success, setSuccess] = useState(false);
    const [serverError, setServerError] = useState("");

    // Cargar categorías
    useEffect(() => {
        const fetchCategories = async () => {
            try {
                const token = localStorage.getItem("token");
                const res = await fetch(`${API}/api/categories`, {
                    headers: { Authorization: `Bearer ${token}` },
                });
                if (!res.ok) throw new Error();
                const data: CategoryDto[] = await res.json();
                setCategories(data.filter((c) => c.isActive));
            } catch {
                // Si falla, el select queda vacío
            } finally {
                setLoadingCats(false);
            }
        };
        fetchCategories();
    }, []);

    const validate = (): boolean => {
        const newErrors: FormErrors = {};

        if (!form.categoryId) {
            newErrors.categoryId = "Selecciona una categoría.";
        }

        if (!form.title.trim()) {
            newErrors.title = "El título es requerido.";
        } else if (form.title.trim().length > 200) {
            newErrors.title = "El título no puede superar los 200 caracteres.";
        }

        if (form.description.length > 1000) {
            newErrors.description =
                "La descripción no puede superar los 1000 caracteres.";
        }

        if (!form.basePrice) {
            newErrors.basePrice = "El precio base es requerido.";
        } else if (
            isNaN(Number(form.basePrice)) ||
            Number(form.basePrice) <= 0.01
        ) {
            newErrors.basePrice = "El precio debe ser mayor a $0.01.";
        }

        if (form.imageUrl && form.imageUrl.length > 2048) {
            newErrors.imageUrl =
                "La URL de la imagen no puede superar los 2048 caracteres.";
        }

        if (form.imageUrl && !/^https?:\/\/.+/.test(form.imageUrl.trim())) {
            newErrors.imageUrl =
                "Ingresa una URL válida (debe comenzar con http:// o https://).";
        }

        setErrors(newErrors);
        return Object.keys(newErrors).length === 0;
    };

    const handleChange = (
        e: React.ChangeEvent<
            HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement
        >,
    ) => {
        const { name, value } = e.target;
        setForm((prev) => ({ ...prev, [name]: value }));
        if (errors[name as keyof FormErrors]) {
            setErrors((prev) => ({ ...prev, [name]: undefined }));
        }
    };

    const handleSubmit = async () => {
        setServerError("");
        if (!validate()) return;

        setSubmitting(true);
        try {
            const token = localStorage.getItem("token");
            const payload = {
                categoryId: Number(form.categoryId),
                title: form.title.trim(),
                description: form.description.trim() || undefined,
                basePrice: Number(form.basePrice),
                imageUrl: form.imageUrl.trim() || undefined,
            };

            const res = await fetch(`${API}/api/services`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    Authorization: `Bearer ${token}`,
                },
                body: JSON.stringify(payload),
            });

            if (res.status === 401) {
                router.push("/login");
                return;
            }

            if (!res.ok) {
                const data = await res.json().catch(() => ({}));
                setServerError(
                    data?.message ??
                        "Ocurrió un error al publicar el servicio.",
                );
                return;
            }

            setSuccess(true);
        } catch {
            setServerError(
                "No se pudo conectar con el servidor. Intenta de nuevo.",
            );
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <div
            style={{
                minHeight: "100vh",
                width: "100%",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                background: "#070a0f",
                padding: "2rem 1rem",
                position: "relative",
                overflow: "hidden",
            }}
        >
            {/* Gradientes de fondo */}
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
                    opacity: 0.4,
                    background:
                        "radial-gradient(circle at top, #7c3aed, transparent 60%)",
                }}
            />

            <div
                style={{
                    position: "relative",
                    width: "100%",
                    maxWidth: "620px",
                }}
            >
                {/* Card */}
                <div
                    style={{
                        position: "relative",
                        borderRadius: "2.5rem",
                        background: "rgba(13,19,27,0.95)",
                        border: "1px solid rgba(255,255,255,0.08)",
                        boxShadow: "0 25px 50px -12px rgba(0,0,0,0.8)",
                        overflow: "hidden",
                    }}
                >
                    {/* Top color section */}
                    <div
                        style={{
                            position: "relative",
                            height: "10rem",
                            background: "#0b0f14",
                        }}
                    >
                        <div
                            style={{
                                position: "absolute",
                                inset: 0,
                                background:
                                    "linear-gradient(to bottom, rgba(109,40,217,0.5), rgba(109,40,217,0.1), transparent)",
                                filter: "blur(24px)",
                                opacity: 0.9,
                            }}
                        />
                        <div
                            style={{
                                position: "absolute",
                                inset: 0,
                                background:
                                    "linear-gradient(to bottom, rgba(11,15,20,0.3), rgba(11,15,20,0.7), #0d131b)",
                            }}
                        />
                    </div>

                    {/* Ícono */}
                    <div
                        style={{
                            display: "flex",
                            justifyContent: "center",
                            marginTop: "-3.5rem",
                            position: "relative",
                            zIndex: 10,
                        }}
                    >
                        <div
                            style={{
                                width: "7rem",
                                height: "7rem",
                                borderRadius: "9999px",
                                background: "#070a0f",
                                border: "4px solid #0d131b",
                                display: "flex",
                                alignItems: "center",
                                justifyContent: "center",
                                boxShadow: "0 20px 25px -5px rgba(0,0,0,0.6)",
                            }}
                        >
                            <div
                                style={{
                                    width: "4rem",
                                    height: "4rem",
                                    borderRadius: "9999px",
                                    background: "rgba(255,255,255,0.05)",
                                    border: "1px solid rgba(255,255,255,0.1)",
                                    display: "flex",
                                    alignItems: "center",
                                    justifyContent: "center",
                                }}
                            >
                                <svg
                                    xmlns="http://www.w3.org/2000/svg"
                                    width="34"
                                    height="34"
                                    viewBox="0 0 24 24"
                                    fill="none"
                                    stroke="white"
                                    strokeWidth="1.5"
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                    style={{ opacity: 0.7 }}
                                >
                                    <path d="M12 20h9" />
                                    <path d="M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z" />
                                </svg>
                            </div>
                        </div>
                    </div>

                    {/* Contenido del formulario */}
                    <div style={{ padding: "1.5rem 3.5rem 3.5rem" }}>
                        {/* Título */}
                        <div
                            style={{
                                textAlign: "center",
                                marginBottom: "2rem",
                            }}
                        >
                            <h1
                                style={{
                                    color: "white",
                                    fontSize: "1.4rem",
                                    fontWeight: 700,
                                    margin: 0,
                                    letterSpacing: "-0.01em",
                                }}
                            >
                                Publicar servicio
                            </h1>
                            <p
                                style={{
                                    color: "rgba(255,255,255,0.4)",
                                    fontSize: "0.875rem",
                                    marginTop: "0.4rem",
                                }}
                            >
                                Completa los datos para ofrecer tu servicio
                            </p>
                        </div>

                        {/* Pantalla de éxito */}
                        {success ? (
                            <div
                                data-testid="success-message"
                                style={{
                                    textAlign: "center",
                                    padding: "2rem 0",
                                    display: "flex",
                                    flexDirection: "column",
                                    alignItems: "center",
                                    gap: "1rem",
                                }}
                            >
                                <div
                                    style={{
                                        width: "4rem",
                                        height: "4rem",
                                        borderRadius: "9999px",
                                        background: "rgba(52,211,153,0.15)",
                                        border: "1px solid rgba(52,211,153,0.4)",
                                        display: "flex",
                                        alignItems: "center",
                                        justifyContent: "center",
                                    }}
                                >
                                    <svg
                                        xmlns="http://www.w3.org/2000/svg"
                                        width="28"
                                        height="28"
                                        viewBox="0 0 24 24"
                                        fill="none"
                                        stroke="#34d399"
                                        strokeWidth="2"
                                        strokeLinecap="round"
                                        strokeLinejoin="round"
                                    >
                                        <polyline points="20 6 9 17 4 12" />
                                    </svg>
                                </div>
                                <p
                                    style={{
                                        color: "#34d399",
                                        fontSize: "1rem",
                                        fontWeight: 600,
                                    }}
                                >
                                    ¡Servicio publicado exitosamente!
                                </p>
                                <p
                                    style={{
                                        color: "rgba(255,255,255,0.4)",
                                        fontSize: "0.875rem",
                                    }}
                                >
                                    Tu servicio ya es visible para los clientes.
                                </p>
                                <button
                                    onClick={() => {
                                        setSuccess(false);
                                        setForm({
                                            categoryId: "",
                                            title: "",
                                            description: "",
                                            basePrice: "",
                                            imageUrl: "",
                                        });
                                    }}
                                    style={{
                                        marginTop: "0.5rem",
                                        padding: "0.75rem 2rem",
                                        borderRadius: "9999px",
                                        border: "1px solid rgba(255,255,255,0.15)",
                                        background: "transparent",
                                        color: "rgba(255,255,255,0.7)",
                                        fontSize: "0.875rem",
                                        cursor: "pointer",
                                        fontFamily: "inherit",
                                    }}
                                >
                                    Publicar otro servicio
                                </button>
                            </div>
                        ) : (
                            <div
                                style={{
                                    display: "flex",
                                    flexDirection: "column",
                                    gap: "1.25rem",
                                }}
                            >
                                {/* Categoría */}
                                <div>
                                    <label style={labelStyle}>Categoría</label>
                                    <select
                                        data-testid="category-select"
                                        name="categoryId"
                                        value={form.categoryId}
                                        onChange={handleChange}
                                        disabled={loadingCats}
                                        style={{
                                            ...inputStyle,
                                            color: form.categoryId
                                                ? "white"
                                                : "rgba(255,255,255,0.35)",
                                            borderColor: errors.categoryId
                                                ? "#f87171"
                                                : "rgba(255,255,255,0.1)",
                                            cursor: loadingCats
                                                ? "not-allowed"
                                                : "pointer",
                                        }}
                                    >
                                        <option
                                            value=""
                                            disabled
                                            style={{
                                                color: "#555",
                                                background: "#05070b",
                                            }}
                                        >
                                            {loadingCats
                                                ? "Cargando categorías..."
                                                : "Selecciona una categoría"}
                                        </option>
                                        {categories.map((cat) => (
                                            <option
                                                key={cat.id}
                                                value={cat.id}
                                                style={{
                                                    color: "white",
                                                    background: "#05070b",
                                                }}
                                            >
                                                {cat.name}
                                            </option>
                                        ))}
                                    </select>
                                    {errors.categoryId && (
                                        <p
                                            data-testid="error-categoryId"
                                            style={errorStyle}
                                        >
                                            {errors.categoryId}
                                        </p>
                                    )}
                                </div>

                                {/* Título */}
                                <div>
                                    <label style={labelStyle}>Título</label>
                                    <input
                                        data-testid="title-input"
                                        type="text"
                                        name="title"
                                        value={form.title}
                                        onChange={handleChange}
                                        placeholder="Ej. Reparación de plomería a domicilio"
                                        maxLength={200}
                                        style={{
                                            ...inputStyle,
                                            borderColor: errors.title
                                                ? "#f87171"
                                                : "rgba(255,255,255,0.1)",
                                        }}
                                        onFocus={(e) => {
                                            e.currentTarget.style.boxShadow =
                                                "0 0 0 2px #7c3aed";
                                            e.currentTarget.style.borderColor =
                                                "#7c3aed";
                                        }}
                                        onBlur={(e) => {
                                            e.currentTarget.style.boxShadow =
                                                "none";
                                            e.currentTarget.style.borderColor =
                                                errors.title
                                                    ? "#f87171"
                                                    : "rgba(255,255,255,0.1)";
                                        }}
                                    />
                                    <div
                                        style={{
                                            display: "flex",
                                            justifyContent: "space-between",
                                            alignItems: "flex-start",
                                            marginTop: "0.35rem",
                                            paddingLeft: "1.2rem",
                                        }}
                                    >
                                        {errors.title ? (
                                            <p
                                                data-testid="error-title"
                                                style={{
                                                    ...errorStyle,
                                                    margin: 0,
                                                    padding: 0,
                                                }}
                                            >
                                                {errors.title}
                                            </p>
                                        ) : (
                                            <span />
                                        )}
                                        <span
                                            style={{
                                                color: "rgba(255,255,255,0.25)",
                                                fontSize: "0.72rem",
                                            }}
                                        >
                                            {form.title.length}/200
                                        </span>
                                    </div>
                                </div>

                                {/* Descripción */}
                                <div>
                                    <label style={labelStyle}>
                                        Descripción (opcional)
                                    </label>
                                    <textarea
                                        data-testid="description-input"
                                        name="description"
                                        value={form.description}
                                        onChange={handleChange}
                                        placeholder="Describe tu servicio con detalle..."
                                        maxLength={1000}
                                        rows={4}
                                        style={{
                                            ...inputStyle,
                                            borderRadius: "1.25rem",
                                            resize: "none",
                                            borderColor: errors.description
                                                ? "#f87171"
                                                : "rgba(255,255,255,0.1)",
                                        }}
                                        onFocus={(e) => {
                                            e.currentTarget.style.boxShadow =
                                                "0 0 0 2px #7c3aed";
                                            e.currentTarget.style.borderColor =
                                                "#7c3aed";
                                        }}
                                        onBlur={(e) => {
                                            e.currentTarget.style.boxShadow =
                                                "none";
                                            e.currentTarget.style.borderColor =
                                                errors.description
                                                    ? "#f87171"
                                                    : "rgba(255,255,255,0.1)";
                                        }}
                                    />
                                    <div
                                        style={{
                                            display: "flex",
                                            justifyContent: "space-between",
                                            marginTop: "0.35rem",
                                            paddingLeft: "1.2rem",
                                        }}
                                    >
                                        {errors.description ? (
                                            <p
                                                data-testid="error-description"
                                                style={{
                                                    ...errorStyle,
                                                    margin: 0,
                                                    padding: 0,
                                                }}
                                            >
                                                {errors.description}
                                            </p>
                                        ) : (
                                            <span />
                                        )}
                                        <span
                                            style={{
                                                color: "rgba(255,255,255,0.25)",
                                                fontSize: "0.72rem",
                                            }}
                                        >
                                            {form.description.length}/1000
                                        </span>
                                    </div>
                                </div>

                                {/* Precio base */}
                                <div>
                                    <label style={labelStyle}>
                                        Precio base (MXN)
                                    </label>
                                    <input
                                        data-testid="price-input"
                                        type="number"
                                        name="basePrice"
                                        value={form.basePrice}
                                        onChange={handleChange}
                                        placeholder="0.00"
                                        min="0.01"
                                        step="0.01"
                                        style={{
                                            ...inputStyle,
                                            borderColor: errors.basePrice
                                                ? "#f87171"
                                                : "rgba(255,255,255,0.1)",
                                        }}
                                        onFocus={(e) => {
                                            e.currentTarget.style.boxShadow =
                                                "0 0 0 2px #7c3aed";
                                            e.currentTarget.style.borderColor =
                                                "#7c3aed";
                                        }}
                                        onBlur={(e) => {
                                            e.currentTarget.style.boxShadow =
                                                "none";
                                            e.currentTarget.style.borderColor =
                                                errors.basePrice
                                                    ? "#f87171"
                                                    : "rgba(255,255,255,0.1)";
                                        }}
                                    />
                                    {errors.basePrice && (
                                        <p
                                            data-testid="error-basePrice"
                                            style={errorStyle}
                                        >
                                            {errors.basePrice}
                                        </p>
                                    )}
                                </div>

                                {/* URL de imagen */}
                                <div>
                                    <label style={labelStyle}>
                                        URL de imagen (opcional)
                                    </label>
                                    <input
                                        data-testid="image-input"
                                        type="url"
                                        name="imageUrl"
                                        value={form.imageUrl}
                                        onChange={handleChange}
                                        placeholder="https://ejemplo.com/imagen.jpg"
                                        style={{
                                            ...inputStyle,
                                            borderColor: errors.imageUrl
                                                ? "#f87171"
                                                : "rgba(255,255,255,0.1)",
                                        }}
                                        onFocus={(e) => {
                                            e.currentTarget.style.boxShadow =
                                                "0 0 0 2px #7c3aed";
                                            e.currentTarget.style.borderColor =
                                                "#7c3aed";
                                        }}
                                        onBlur={(e) => {
                                            e.currentTarget.style.boxShadow =
                                                "none";
                                            e.currentTarget.style.borderColor =
                                                errors.imageUrl
                                                    ? "#f87171"
                                                    : "rgba(255,255,255,0.1)";
                                        }}
                                    />
                                    {errors.imageUrl && (
                                        <p
                                            data-testid="error-imageUrl"
                                            style={errorStyle}
                                        >
                                            {errors.imageUrl}
                                        </p>
                                    )}

                                    {/* Preview */}
                                    {form.imageUrl &&
                                        !errors.imageUrl &&
                                        /^https?:\/\/.+/.test(
                                            form.imageUrl.trim(),
                                        ) && (
                                            <ImagePreview
                                                url={form.imageUrl.trim()}
                                            />
                                        )}
                                </div>

                                {/* Error de servidor */}
                                {serverError && (
                                    <p
                                        data-testid="server-error"
                                        style={{
                                            ...errorStyle,
                                            textAlign: "center",
                                            paddingLeft: 0,
                                            fontSize: "0.875rem",
                                        }}
                                    >
                                        {serverError}
                                    </p>
                                )}

                                {/* Botón submit */}
                                <button
                                    data-testid="submit-button"
                                    type="button"
                                    onClick={handleSubmit}
                                    disabled={submitting}
                                    style={{
                                        display: "block",
                                        width: "100%",
                                        boxSizing: "border-box",
                                        padding: "1rem",
                                        borderRadius: "9999px",
                                        border: "none",
                                        background: submitting
                                            ? "rgba(124,58,237,0.5)"
                                            : "linear-gradient(to right, #6d28d9, #7c3aed)",
                                        color: "white",
                                        fontSize: "1rem",
                                        fontWeight: 600,
                                        cursor: submitting
                                            ? "not-allowed"
                                            : "pointer",
                                        boxShadow:
                                            "0 10px 15px -3px rgba(124,58,237,0.3)",
                                        fontFamily: "inherit",
                                        letterSpacing: "0.05em",
                                        marginTop: "0.5rem",
                                        transition: "background 0.2s",
                                    }}
                                    onMouseEnter={(e) => {
                                        if (!submitting)
                                            e.currentTarget.style.background =
                                                "linear-gradient(to right, #5b21b6, #6d28d9)";
                                    }}
                                    onMouseLeave={(e) => {
                                        if (!submitting)
                                            e.currentTarget.style.background =
                                                "linear-gradient(to right, #6d28d9, #7c3aed)";
                                    }}
                                >
                                    {submitting
                                        ? "PUBLICANDO..."
                                        : "PUBLICAR SERVICIO"}
                                </button>
                            </div>
                        )}
                    </div>

                    {/* Línea inferior */}
                    <div
                        style={{
                            position: "absolute",
                            bottom: 0,
                            left: 0,
                            width: "100%",
                            height: "3px",
                            background:
                                "linear-gradient(to right, transparent, #a855f7, transparent)",
                            opacity: 0.8,
                        }}
                    />
                </div>
            </div>
        </div>
    );
}
