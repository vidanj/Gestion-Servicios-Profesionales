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
    const [error, setError] = useState("");

    const validateForm = () => {
        if (!firstName.trim()) {
            setError("El nombre es obligatorio");
            return false;
        }
        if (!lastName.trim()) {
            setError("El apellido es obligatorio");
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
        setError("");
        if (!validateForm()) return;

        try {
            const res = await fetch("http://localhost:5000/api/Auth/register", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ email, password, firstName, lastName, phoneNumber }),
            });

            if (!res.ok) {
                setError("Error al registrar");
                return;
            }

            const data = await res.json();
            localStorage.setItem("token", data.token);
            router.push("/dashboard");
        } catch (error) {
            console.error(error);
            setError("Error de conexión");
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
                                style={inputStyle}
                                onFocus={handleFocus}
                                onBlur={handleBlur}
                            />

                            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", fontSize: "0.875rem" }}>
                                <Link href="/login" style={{ color: "rgba(255,255,255,0.8)", textDecoration: "none", fontWeight: 500 }}>
                                    ¿Ya tienes cuenta?
                                </Link>
                                <Link href="/recovery" style={{ color: "rgba(255,255,255,0.8)", textDecoration: "none", fontWeight: 500 }}>
                                    ¿Olvidaste tu contraseña?
                                </Link>
                            </div>

                            <button
                                data-testid="register-button"
                                type="button"
                                onClick={handleRegister}
                                style={{ display: "block", width: "100%", boxSizing: "border-box", padding: "1rem", borderRadius: "9999px", border: "none", background: "linear-gradient(to right, #6d28d9, #7c3aed)", color: "white", fontSize: "1.125rem", fontWeight: 600, cursor: "pointer", boxShadow: "0 10px 15px -3px rgba(124,58,237,0.3)", fontFamily: "inherit", letterSpacing: "0.05em" }}
                                onMouseEnter={(e) => { e.currentTarget.style.background = "linear-gradient(to right, #5b21b6, #6d28d9)"; }}
                                onMouseLeave={(e) => { e.currentTarget.style.background = "linear-gradient(to right, #6d28d9, #7c3aed)"; }}
                            >
                                Registrarme
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