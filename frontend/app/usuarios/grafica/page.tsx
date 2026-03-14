'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';

interface RegistrationStat {
  date: string;
  count: number;
}

const DAYS_OPTIONS = [7, 14, 30, 90];

const API = process.env.NEXT_PUBLIC_ALLOWED_PATH;

export default function GraficaPage() {
  const pathname = usePathname();
  const [stats, setStats] = useState<RegistrationStat[]>([]);
  const [days, setDays] = useState(30);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);

  useEffect(() => {
    const fetchStats = async () => {
      setLoading(true);
      setError(false);
      try {
        const token = localStorage.getItem('token');
        const res = await fetch(`${API}/api/Users/stats/registrations?days=${days}`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        if (!res.ok) throw new Error();
        const data: RegistrationStat[] = await res.json();
        setStats(data);
      } catch {
        setError(true);
      } finally {
        setLoading(false);
      }
    };
    fetchStats();
  }, [days]);

  const navLinks = [
    { href: '/usuarios', label: 'CRUD' },
    { href: '/usuarios/registrados', label: 'Usuarios registrados' },
    { href: '/usuarios/logs', label: 'Logs de usuarios' },
    { href: '/usuarios/grafica', label: 'Gráfica' },
  ];

  // SVG chart dimensions
  const W = 700;
  const H = 260;
  const PAD = { top: 20, right: 20, bottom: 50, left: 45 };
  const chartW = W - PAD.left - PAD.right;
  const chartH = H - PAD.top - PAD.bottom;

  const maxCount = Math.max(...stats.map(s => s.count), 1);
  const totalRegistros = stats.reduce((sum, s) => sum + s.count, 0);

  const barWidth = stats.length > 0 ? (chartW / stats.length) * 0.6 : 0;
  const barGap = stats.length > 0 ? chartW / stats.length : 0;

  const yTicks = 5;

  return (
    <div style={{ backgroundColor: '#0d1117', minHeight: '100vh', padding: '2rem' }}>
      {/* Nav */}
      <div style={{ display: 'flex', gap: '1rem', marginBottom: '2rem', flexWrap: 'wrap' }}>
        {navLinks.map(link => (
          <Link
            key={link.href}
            href={link.href}
            style={{
              padding: '0.5rem 1.2rem',
              borderRadius: '8px',
              backgroundColor: pathname === link.href ? '#7c3aed' : 'transparent',
              color: pathname === link.href ? '#fff' : '#888',
              textDecoration: 'none',
              fontWeight: 500,
              fontSize: '0.9rem',
            }}
          >
            {link.label}
          </Link>
        ))}
      </div>

      {/* Header */}
      <div style={{ marginBottom: '1.5rem' }}>
        <h1 style={{ color: '#fff', fontSize: '1.3rem', fontWeight: 700, margin: 0 }}>
          Gráfica de registros
        </h1>
        <p style={{ color: '#888', fontSize: '0.9rem', marginTop: '0.3rem' }}>
          Usuarios registrados por fecha.
        </p>
      </div>

      {/* Card */}
      <div style={{
        backgroundColor: '#fff',
        borderRadius: '12px',
        padding: '2rem',
        maxWidth: '800px',
      }}>
        {/* Filtro días */}
        <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '1.5rem', alignItems: 'center' }}>
          <span style={{ fontSize: '0.85rem', color: '#555', marginRight: '0.5rem' }}>Período:</span>
          {DAYS_OPTIONS.map(d => (
            <button
              key={d}
              data-testid={`filter-days-${d}`}
              onClick={() => setDays(d)}
              style={{
                padding: '0.35rem 0.9rem',
                borderRadius: '6px',
                border: 'none',
                cursor: 'pointer',
                fontSize: '0.85rem',
                backgroundColor: days === d ? '#7c3aed' : '#f3f4f6',
                color: days === d ? '#fff' : '#555',
                fontWeight: days === d ? 600 : 400,
              }}
            >
              {d}d
            </button>
          ))}
          <span style={{ marginLeft: 'auto', fontSize: '0.85rem', color: '#888' }}>
            Total: <strong>{totalRegistros}</strong> usuarios
          </span>
        </div>

        {/* Estados */}
        {loading && (
          <div data-testid="grafica-loading" style={{ textAlign: 'center', padding: '3rem', color: '#888' }}>
            Cargando...
          </div>
        )}

        {error && (
          <div data-testid="grafica-error" style={{ textAlign: 'center', padding: '3rem', color: '#ef4444' }}>
            No se pudo cargar la gráfica.
          </div>
        )}

        {/* SVG Chart */}
        {!loading && !error && stats.length > 0 && (
          <svg
            data-testid="grafica-svg"
            viewBox={`0 0 ${W} ${H}`}
            style={{ width: '100%', height: 'auto' }}
          >
            {/* Y axis ticks & grid lines */}
            {Array.from({ length: yTicks + 1 }, (_, i) => {
              const val = Math.round((maxCount / yTicks) * i);
              const y = PAD.top + chartH - (chartH * i) / yTicks;
              return (
                <g key={i}>
                  <line
                    x1={PAD.left} y1={y}
                    x2={PAD.left + chartW} y2={y}
                    stroke="#e5e7eb" strokeWidth="1"
                  />
                  <text
                    x={PAD.left - 8} y={y + 4}
                    textAnchor="end" fontSize="11" fill="#9ca3af"
                  >
                    {val}
                  </text>
                </g>
              );
            })}

            {/* Bars */}
            {stats.map((s, i) => {
              const barH = maxCount > 0 ? (s.count / maxCount) * chartH : 0;
              const x = PAD.left + i * barGap + (barGap - barWidth) / 2;
              const y = PAD.top + chartH - barH;

              // Show label every N bars to avoid overlap
              const labelEvery = Math.ceil(stats.length / 15);
              const showLabel = i % labelEvery === 0;
              const shortDate = s.date.slice(5); // MM-DD

              return (
                <g key={s.date}>
                  <rect
                    data-testid="grafica-bar"
                    x={x} y={y}
                    width={barWidth} height={barH}
                    rx="3"
                    fill="#7c3aed"
                    opacity="0.85"
                  />
                  {s.count > 0 && (
                    <text
                      x={x + barWidth / 2} y={y - 5}
                      textAnchor="middle" fontSize="10" fill="#7c3aed" fontWeight="600"
                    >
                      {s.count}
                    </text>
                  )}
                  {showLabel && (
                    <text
                      x={x + barWidth / 2}
                      y={PAD.top + chartH + 18}
                      textAnchor="middle" fontSize="10" fill="#6b7280"
                      transform={`rotate(-35, ${x + barWidth / 2}, ${PAD.top + chartH + 18})`}
                    >
                      {shortDate}
                    </text>
                  )}
                </g>
              );
            })}

            {/* Axes */}
            <line
              x1={PAD.left} y1={PAD.top}
              x2={PAD.left} y2={PAD.top + chartH}
              stroke="#d1d5db" strokeWidth="1.5"
            />
            <line
              x1={PAD.left} y1={PAD.top + chartH}
              x2={PAD.left + chartW} y2={PAD.top + chartH}
              stroke="#d1d5db" strokeWidth="1.5"
            />
          </svg>
        )}
      </div>
    </div>
  );
}
