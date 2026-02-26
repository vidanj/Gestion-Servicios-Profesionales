"use client";
import { useRouter } from "next/navigation";
import Link from "next/link";

export default function LoginPage() {
  const router = useRouter();
  return (
    <div style={{
      minHeight: "100vh",
      width: "100%",
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
      background: "#070a0f",
      padding: "0 1rem",
      position: "relative",
      overflow: "hidden",
    }}>
      {/* Gradient backgrounds */}
      <div style={{
        position: "absolute", inset: 0,
        background: "linear-gradient(to bottom, rgba(43,19,95,0.7), #070a0f, #070a0f)",
      }} />
      <div style={{
        position: "absolute", inset: 0, opacity: 0.4,
        background: "radial-gradient(circle at top, #7c3aed, transparent 60%)",
      }} />

      <div style={{ position: "relative", width: "100%", maxWidth: "700px" }}>
        {/* Card */}
        <div style={{
          position: "relative",
          borderRadius: "2.5rem",
          background: "rgba(13,19,27,0.95)",
          border: "1px solid rgba(255,255,255,0.08)",
          boxShadow: "0 25px 50px -12px rgba(0,0,0,0.8)",
          overflow: "hidden",
        }}>
          {/* Top color section */}
          <div style={{ position: "relative", height: "10rem", background: "#0b0f14" }}>
            <div style={{
              position: "absolute", inset: 0,
              background: "linear-gradient(to bottom, rgba(109,40,217,0.5), rgba(109,40,217,0.1), transparent)",
              filter: "blur(24px)", opacity: 0.9,
            }} />
            <div style={{
              position: "absolute", inset: 0,
              background: "linear-gradient(to bottom, rgba(11,15,20,0.3), rgba(11,15,20,0.7), #0d131b)",
            }} />
          </div>

          {/* Avatar */}
          <div style={{ display: "flex", justifyContent: "center", marginTop: "-3.5rem", position: "relative", zIndex: 10 }}>
            <div style={{
              width: "7rem", height: "7rem", borderRadius: "9999px",
              background: "#070a0f", border: "4px solid #0d131b",
              display: "flex", alignItems: "center", justifyContent: "center",
              boxShadow: "0 20px 25px -5px rgba(0,0,0,0.6)",
            }}>
              <div style={{
                width: "4rem", height: "4rem", borderRadius: "9999px",
                background: "rgba(255,255,255,0.05)", border: "1px solid rgba(255,255,255,0.1)",
                display: "flex", alignItems: "center", justifyContent: "center",
              }}>
                <svg xmlns="http://www.w3.org/2000/svg" width="34" height="34"
                  viewBox="0 0 24 24" fill="none" stroke="white"
                  strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"
                  style={{ opacity: 0.7 }} suppressHydrationWarning>
                  <path d="M20 21a8 8 0 0 0-16 0" />
                  <circle cx="12" cy="7" r="4" />
                </svg>
              </div>
            </div>
          </div>

          {/* Form */}
          <div style={{ padding: "2.5rem 5rem 3.5rem" }}>
            <form style={{ display: "flex", flexDirection: "column", gap: "1.75rem" }}>
              {/* Username */}
              <input
                data-testid="username-input"
                type="text"
                placeholder="username"
                style={{
                  display: "block", width: "100%", boxSizing: "border-box",
                  padding: "1.25rem 1.5rem",
                  borderRadius: "9999px",
                  background: "#05070b",
                  border: "1px solid rgba(255,255,255,0.1)",
                  color: "white",
                  fontSize: "1.125rem",
                  outline: "none",
                  fontFamily: "inherit",
                }}
                onFocus={e => { e.currentTarget.style.boxShadow = "0 0 0 2px #7c3aed"; e.currentTarget.style.borderColor = "#7c3aed"; }}
                onBlur={e => { e.currentTarget.style.boxShadow = "none"; e.currentTarget.style.borderColor = "rgba(255,255,255,0.1)"; }}
              />

              {/* Password */}
              <input
                data-testid="password-input"
                type="password"
                placeholder="password"
                style={{
                  display: "block", width: "100%", boxSizing: "border-box",
                  padding: "1.25rem 1.5rem",
                  borderRadius: "9999px",
                  background: "#05070b",
                  border: "1px solid rgba(255,255,255,0.1)",
                  color: "white",
                  fontSize: "1.125rem",
                  outline: "none",
                  fontFamily: "inherit",
                }}
                onFocus={e => { e.currentTarget.style.boxShadow = "0 0 0 2px #7c3aed"; e.currentTarget.style.borderColor = "#7c3aed"; }}
                onBlur={e => { e.currentTarget.style.boxShadow = "none"; e.currentTarget.style.borderColor = "rgba(255,255,255,0.1)"; }}
              />

              {/* Remember + Forgot */}
              <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", fontSize: "0.875rem" }}>
                <label style={{ display: "flex", alignItems: "center", gap: "0.5rem", color: "rgba(255,255,255,0.5)", cursor: "pointer" }}>
                  <input type="checkbox" style={{ accentColor: "#7c3aed", width: "1rem", height: "1rem", cursor: "pointer" }} />
                  Remember me
                </label>
                <Link href="/recovery" style={{ color: "rgba(255,255,255,0.8)", textDecoration: "none", fontWeight: 500 }}>
                  Forgot your password?
                </Link>
              </div>

              {/* Login button */}
              <button
                data-testid="login-button"
                type="button"
                onClick={() => router.push("/dashboard")}
                style={{
                  display: "block", width: "100%", boxSizing: "border-box",
                  padding: "1rem",
                  borderRadius: "9999px",
                  border: "none",
                  background: "linear-gradient(to right, #6d28d9, #7c3aed)",
                  color: "white",
                  fontSize: "1.125rem",
                  fontWeight: 600,
                  cursor: "pointer",
                  boxShadow: "0 10px 15px -3px rgba(124,58,237,0.3)",
                  fontFamily: "inherit",
                  letterSpacing: "0.05em",
                }}
                onMouseEnter={e => { e.currentTarget.style.background = "linear-gradient(to right, #5b21b6, #6d28d9)"; }}
                onMouseLeave={e => { e.currentTarget.style.background = "linear-gradient(to right, #6d28d9, #7c3aed)"; }}
              >
                LOGIN
              </button>
            </form>
          </div>

          {/* Bottom glow line */}
          <div style={{
            position: "absolute", bottom: 0, left: 0, width: "100%", height: "3px",
            background: "linear-gradient(to right, transparent, #a855f7, transparent)",
            opacity: 0.8,
          }} />
        </div>
      </div>
    </div>
  );
}
