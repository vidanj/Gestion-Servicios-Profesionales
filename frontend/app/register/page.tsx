"use client";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useState } from "react";

export default function RegisterPage() {
    const router = useRouter();
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [firstName, setFirstName] = useState("");
    const [lastName, setLastName] = useState("");
    const [phoneNumber, setPhoneNumber] = useState("");
    const [role, setRole] = useState("");
    const [isLoading, setIsLoading] = useState(false);
    const [loaderImageError, setLoaderImageError] = useState(false);

    const [roleOpen, setRoleOpen] = useState(false);

    const roles = [
        { value: "1", label: "Cliente" },
        { value: "2", label: "Profesionista" },
    ];

    const selectedRole = roles.find(r => r.value === role);

    const [error, setError] = useState("");
    const apiUrl = process.env.NEXT_PUBLIC_ALLOWED_PATH || "http://localhost:5000";

    const validateForm = () => {
        if (!firstName.trim()) {
            setError("El nombre es obligatorio");
            return false;
        }
        if (!lastName.trim()) {
            setError("El apellido es obligatorio");
            return false;
        }
        if (!role.trim()) {
            setError("El rol es obligatorio");
            return false;
        }
        if (!email.trim()) {
            setError("El email es obligatorio");
            return false;
        }
        if (!email.includes("@")) {
            setError("El email no es válido");
            return false;
        }
        if (!phoneNumber.trim()) {
            setError("El teléfono es obligatorio");
            return false;
        }
        if (!password.trim()) {
            setError("La contraseña es obligatoria");
            return false;
        }
        if (password.length < 6) {
            setError("La contraseña debe tener al menos 6 caracteres");
            return false;
        }
        setError("");
        return true;
    };

    const handleRegister = async () => {
        if (isLoading) return;

        setError("");
        if (!validateForm()) return;
        setIsLoading(true);

        try {
            const res = await fetch(`${apiUrl}/api/Auth/register`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ email, password, firstName, lastName, phoneNumber, role: parseInt(role) }),
            });

            if (!res.ok) {
                const responseError = await res.json().catch(() => null);
                setError(responseError?.message || "Error al registrar");
                return;
            }

            const data = await res.json();
            localStorage.setItem("token", data.token);
            router.push("/login");
        } catch (error) {
            console.error(error);
            setError("Error de conexión");
        } finally {
            setIsLoading(false);
        }
    };

    const inputStyle = {
        display: "block", width: "100%", boxSizing: "border-box" as const,
        padding: "1.25rem 1.5rem", borderRadius: "9999px",
        background: "#05070b", border: "1px solid rgba(255,255,255,0.1)",
        color: "white", fontSize: "1.125rem", outline: "none", fontFamily: "inherit",
    };

    const handleFocus = (e: React.FocusEvent<HTMLInputElement>) => {
        e.currentTarget.style.boxShadow = "0 0 0 2px #7c3aed";
        e.currentTarget.style.borderColor = "#7c3aed";
    };

    const handleBlur = (e: React.FocusEvent<HTMLInputElement>) => {
        e.currentTarget.style.boxShadow = "none";
        e.currentTarget.style.borderColor = "rgba(255,255,255,0.1)";
    };

    return (
        <div style={{
            minHeight: "100vh", width: "100%", display: "flex",
            alignItems: "center", justifyContent: "center",
            background: "#070a0f", padding: "0 1rem",
            position: "relative", overflow: "hidden",
        }}>
            <div style={{ position: "absolute", inset: 0, background: "linear-gradient(to bottom, rgba(43,19,95,0.7), #070a0f, #070a0f)" }} />
            <div style={{ position: "absolute", inset: 0, opacity: 0.4, background: "radial-gradient(circle at top, #7c3aed, transparent 60%)" }} />

            <div style={{ position: "relative", width: "100%", maxWidth: "700px" }}>
                <div style={{
                    position: "relative", borderRadius: "2.5rem",
                    background: "rgba(13,19,27,0.95)", border: "1px solid rgba(255,255,255,0.08)",
                    boxShadow: "0 25px 50px -12px rgba(0,0,0,0.8)", overflow: "hidden",
                }}>
                    {isLoading && (
                        <div
                            data-testid="register-loading-overlay"
                            style={{
                                position: "absolute",
                                inset: 0,
                                zIndex: 30,
                                display: "flex",
                                flexDirection: "column",
                                alignItems: "center",
                                justifyContent: "center",
                                gap: "0.75rem",
                                background: "rgba(7,10,15,0.75)",
                                backdropFilter: "blur(2px)",
                            }}
                        >
                            {!loaderImageError ? (
                                <img
                                    src="/blocks-shuffle-3.svg"
                                    alt="Registrando"
                                    style={{ width: "84px", height: "84px", objectFit: "contain" }}
                                    onError={() => setLoaderImageError(true)}
                                />
                            ) : (
                                <div
                                    aria-label="Registrando"
                                    style={{
                                        width: "56px",
                                        height: "56px",
                                        borderRadius: "9999px",
                                        border: "4px solid rgba(255,255,255,0.25)",
                                        borderTopColor: "#a855f7",
                                        animation: "register-spin 0.9s linear infinite",
                                    }}
                                />
                            )}
                            <p style={{ margin: 0, color: "white", fontWeight: 600 }}>Registrando...</p>
                        </div>
                    )}

                    <style>{`@keyframes register-spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }`}</style>

                    <div style={{ position: "relative", height: "10rem", background: "#0b0f14" }}>
                        <div style={{ position: "absolute", inset: 0, background: "linear-gradient(to bottom, rgba(109,40,217,0.5), rgba(109,40,217,0.1), transparent)", filter: "blur(24px)", opacity: 0.9 }} />
                        <div style={{ position: "absolute", inset: 0, background: "linear-gradient(to bottom, rgba(11,15,20,0.3), rgba(11,15,20,0.7), #0d131b)" }} />
                    </div>

                    <div style={{ display: "flex", justifyContent: "center", marginTop: "-3.5rem", position: "relative", zIndex: 10 }}>
                        <div style={{ width: "7rem", height: "7rem", borderRadius: "9999px", background: "#070a0f", border: "4px solid #0d131b", display: "flex", alignItems: "center", justifyContent: "center", boxShadow: "0 20px 25px -5px rgba(0,0,0,0.6)" }}>
                            <div style={{ width: "4rem", height: "4rem", borderRadius: "9999px", background: "rgba(255,255,255,0.05)", border: "1px solid rgba(255,255,255,0.1)", display: "flex", alignItems: "center", justifyContent: "center" }}>
                                <svg xmlns="http://www.w3.org/2000/svg" width="34" height="34" viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" style={{ opacity: 0.7 }} suppressHydrationWarning>
                                    <path d="M20 21a8 8 0 0 0-16 0" />
                                    <circle cx="12" cy="7" r="4" />
                                </svg>
                            </div>
                        </div>
                    </div>

                    <div style={{ padding: "2.5rem 5rem 3.5rem" }}>
                        <form style={{ display: "flex", flexDirection: "column", gap: "1.75rem" }}>

                            <input
                                data-testid="firstname-input"
                                type="text"
                                placeholder="nombre"
                                value={firstName}
                                onChange={(e) => setFirstName(e.target.value)}
                                disabled={isLoading}
                                style={inputStyle}
                                onFocus={handleFocus}
                                onBlur={handleBlur}
                            />

                            <input
                                data-testid="lastname-input"
                                type="text"
                                placeholder="apellido"
                                value={lastName}
                                onChange={(e) => setLastName(e.target.value)}
                                disabled={isLoading}
                                style={inputStyle}
                                onFocus={handleFocus}
                                onBlur={handleBlur}
                            />

                            <input
                                data-testid="username-input"
                                type="email"
                                placeholder="correo electrónico"
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}
                                disabled={isLoading}
                                style={inputStyle}
                                onFocus={handleFocus}
                                onBlur={handleBlur}
                            />

                            <input
                                data-testid="phone-input"
                                type="tel"
                                placeholder="teléfono"
                                value={phoneNumber}
                                onChange={(e) => setPhoneNumber(e.target.value)}
                                disabled={isLoading}
                                style={inputStyle}
                                onFocus={handleFocus}
                                onBlur={handleBlur}
                            />

                            <input
                                data-testid="password-input"
                                type="password"
                                placeholder="contraseña"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                disabled={isLoading}
                                style={inputStyle}
                                onFocus={handleFocus}
                                onBlur={handleBlur}
                            />

                            <div style={{ position: "relative" }}>
                                <button
                                    data-testid="role-button"
                                    type="button"
                                    onClick={() => setRoleOpen(!roleOpen)}
                                    disabled={isLoading}
                                    style={{
                                        ...inputStyle,
                                        display: "flex",
                                        alignItems: "center",
                                        justifyContent: "space-between",
                                        cursor: isLoading ? "default" : "pointer",
                                        color: selectedRole ? "white" : "rgba(255,255,255,0.4)",
                                        boxShadow: roleOpen ? "0 0 0 2px #7c3aed" : "none",
                                        borderColor: roleOpen ? "#7c3aed" : "rgba(255,255,255,0.1)",
                                        opacity: isLoading ? 0.8 : 1,
                                    }}
                                >
                                    <span>{selectedRole ? selectedRole.label : "rol"}</span>
                                    <svg
                                        xmlns="http://www.w3.org/2000/svg"
                                        width="18" height="18"
                                        viewBox="0 0 24 24"
                                        fill="none" stroke="rgba(255,255,255,0.4)"
                                        strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"
                                        style={{ transform: roleOpen ? "rotate(180deg)" : "rotate(0deg)", transition: "transform 0.2s" }}
                                    >
                                        <polyline points="6 9 12 15 18 9" />
                                    </svg>
                                </button>

                                {roleOpen && (
                                    <div style={{
                                        position: "absolute", top: "calc(100% + 8px)", left: 0, right: 0,
                                        background: "#0d131b", border: "1px solid rgba(255,255,255,0.1)",
                                        borderRadius: "1.25rem", overflow: "hidden",
                                        boxShadow: "0 20px 25px -5px rgba(0,0,0,0.6)", zIndex: 50,
                                    }}>
                                        {roles.map((r) => (
                                            <button
                                                data-testid={`role-option-${r.label.toLowerCase()}`}
                                                key={r.value}
                                                type="button"
                                                onClick={() => { setRole(r.value); setRoleOpen(false); }}
                                                style={{
                                                    display: "block", width: "100%", textAlign: "left",
                                                    padding: "1rem 1.5rem", background: "transparent",
                                                    border: "none", color: role === r.value ? "#a78bfa" : "white",
                                                    fontSize: "1.125rem", cursor: "pointer", fontFamily: "inherit",
                                                    borderBottom: "1px solid rgba(255,255,255,0.05)",
                                                }}
                                                onMouseEnter={(e) => { e.currentTarget.style.background = "rgba(124,58,237,0.15)"; }}
                                                onMouseLeave={(e) => { e.currentTarget.style.background = "transparent"; }}
                                            >
                                                {r.value === role && "✓ "}{r.label}
                                            </button>
                                        ))}
                                    </div>
                                )}
                            </div>

                            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", fontSize: "0.875rem" }}>
                                <Link href="/login" style={{ color: "rgba(255,255,255,0.8)", textDecoration: "none", fontWeight: 500 }}>
                                    ¿Ya tienes cuenta?
                                </Link>
                            </div>

                            <button
                                data-testid="register-button"
                                type="button"
                                onClick={handleRegister}
                                disabled={isLoading}
                                style={{ display: "block", width: "100%", boxSizing: "border-box", padding: "1rem", borderRadius: "9999px", border: "none", background: "linear-gradient(to right, #6d28d9, #7c3aed)", color: "white", fontSize: "1.125rem", fontWeight: 600, cursor: isLoading ? "default" : "pointer", boxShadow: "0 10px 15px -3px rgba(124,58,237,0.3)", fontFamily: "inherit", letterSpacing: "0.05em", opacity: isLoading ? 0.8 : 1, pointerEvents: isLoading ? "none" : "auto" }}
                                onMouseEnter={(e) => { if (!isLoading) e.currentTarget.style.background = "linear-gradient(to right, #5b21b6, #6d28d9)"; }}
                                onMouseLeave={(e) => { if (!isLoading) e.currentTarget.style.background = "linear-gradient(to right, #6d28d9, #7c3aed)"; }}
                            >
                                {isLoading ? "REGISTRANDO..." : "Registrarme"}
                            </button>

                            {error && <p data-testid="error-message" style={{ color: "red" }}>{error}</p>}
                        </form>
                    </div>

                    <div style={{ position: "absolute", bottom: 0, left: 0, width: "100%", height: "3px", background: "linear-gradient(to right, transparent, #a855f7, transparent)", opacity: 0.8 }} />
                </div>
            </div>
        </div>
    );
}