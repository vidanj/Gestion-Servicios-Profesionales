"use client";
import Link from "next/link";

export default function AboutPage() {
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
                  style={{ opacity: 0.7 }}>
                  <path d="M20 21a8 8 0 0 0-16 0" />
                  <circle cx="12" cy="7" r="4" />
                </svg>
              </div>
            </div>
          </div>

          {/* Content */}
          <div style={{ padding: "2.5rem 5rem 3.5rem", color: "white" }}>
            <div style={{ display: "flex", flexDirection: "column", gap: "1.5rem" }}>
              
              {/* Title */}
              <h1 style={{
                textAlign: "center",
                fontSize: "1.8rem",
                fontWeight: 700,
                letterSpacing: "0.05em"
              }}>
                About Our Marketplace
              </h1>

              {/* Description */}
              <p style={{
                textAlign: "center",
                color: "rgba(255,255,255,0.65)",
                lineHeight: 1.6,
                fontSize: "0.95rem"
              }}>
                We connect businesses with talented professionals worldwide.
                Our platform enables clients to discover, hire, and collaborate
                with experts across design, development, marketing, and more.
              </p>

              {/* Stats */}
              <div style={{
                display: "flex",
                justifyContent: "space-between",
                gap: "1rem",
                marginTop: "0.5rem"
              }}>
                {[
                  { label: "Freelancers", value: "12K+" },
                  { label: "Projects", value: "48K+" },
                  { label: "Clients", value: "9K+" }
                ].map((s) => (
                  <div key={s.label} style={{
                    flex: 1,
                    borderRadius: "1.25rem",
                    background: "rgba(255,255,255,0.03)",
                    border: "1px solid rgba(255,255,255,0.06)",
                    padding: "1rem",
                    textAlign: "center"
                  }}>
                    <div style={{ fontSize: "1.2rem", fontWeight: 700 }}>
                      {s.value}
                    </div>
                    <div style={{
                      fontSize: "0.75rem",
                      color: "rgba(255,255,255,0.5)",
                      marginTop: "0.25rem"
                    }}>
                      {s.label}
                    </div>
                  </div>
                ))}
              </div>

              {/* Mission */}
              <p style={{
                textAlign: "center",
                color: "rgba(255,255,255,0.6)",
                fontSize: "0.9rem",
                marginTop: "0.5rem"
              }}>
                Our mission is to empower independent professionals and help
                companies scale faster through trusted digital services.
              </p>

              {/* CTA */}
              <Link
                href="/login"
                style={{
                  display: "block",
                  textAlign: "center",
                  marginTop: "0.75rem",
                  padding: "0.9rem",
                  borderRadius: "9999px",
                  background: "linear-gradient(to right, #6d28d9, #7c3aed)",
                  color: "white",
                  fontWeight: 600,
                  letterSpacing: "0.05em",
                  textDecoration: "none"
                }}
              >
                GET STARTED
              </Link>

            </div>
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