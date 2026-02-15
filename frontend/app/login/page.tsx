"use client";

import Link from "next/link";

export default function LoginPage() {
  return (
    <div className="min-h-screen w-full flex items-center justify-center bg-[#070a0f] px-4 relative overflow-hidden">
      
      {/* Fondo con glow morado suave */}
      <div className="absolute inset-0 bg-gradient-to-b from-[#2b135f]/70 via-[#070a0f] to-[#070a0f]" />
      <div className="absolute inset-0 opacity-40 bg-[radial-gradient(circle_at_top,#7c3aed,transparent_60%)]" />

      <div className="relative w-full max-w-2xl flex justify-center">
        
        {/* Card */}
        <div className="relative w-[700px] rounded-[2.5rem] bg-[#0d131b]/90 border border-white/10 shadow-2xl overflow-hidden">

          {/* TOP SECTION */}
          <div className="relative h-48 bg-[#0b0f14]">
            {/* blur glow arriba */}
            <div className="absolute inset-0 bg-gradient-to-b from-purple-700/40 via-purple-700/10 to-transparent blur-2xl opacity-90" />
            {/* overlay oscuro */}
            <div className="absolute inset-0 bg-gradient-to-b from-[#0b0f14]/30 via-[#0b0f14]/70 to-[#0d131b]" />
          </div>

          {/* Avatar */}
          <div className="flex justify-center -mt-14 relative z-10">
            <div className="w-28 h-28 rounded-full bg-[#070a0f] border-4 border-[#0d131b] flex items-center justify-center shadow-xl shadow-black/60">
              <div className="w-16 h-16 rounded-full bg-white/5 flex items-center justify-center border border-white/10">
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
                  className="opacity-70"
                >
                  <path d="M20 21a8 8 0 0 0-16 0" />
                  <circle cx="12" cy="7" r="4" />
                </svg>
              </div>
            </div>
          </div>

          {/* Form */}
          <div className="px-20 pt-10 pb-14">
            <form className="space-y-7">

              {/* Username */}
              <div className="relative">
                <input
                  type="text"
                  placeholder="username"
                  className="w-full pl-14 pr-6 py-5 rounded-full bg-[#05070b] border border-white/10 text-white text-lg placeholder:text-white/25 focus:outline-none focus:ring-2 focus:ring-purple-600 focus:border-purple-500 transition"
                />
              </div>

              {/* Password */}
              <div className="relative">
                <input
                  type="password"
                  placeholder="password"
                  className="w-full pl-14 pr-6 py-5 rounded-full bg-[#05070b] border border-white/10 text-white text-lg placeholder:text-white/25 focus:outline-none focus:ring-2 focus:ring-purple-600 focus:border-purple-500 transition"
                />
              </div>

              {/* Remember + Forgot */}
              <div className="flex items-center justify-between text-sm mt-2">
                <label className="flex items-center gap-2 text-white/50 cursor-pointer">
                  <input type="checkbox" className="accent-purple-600 w-4 h-4" />
                  Remember me
                </label>

                <Link
                  href="#"
                  className="text-white/90 hover:text-purple-300 transition font-medium"
                >
                  Forgot your password?
                </Link>
              </div>

              {/* Login button */}
              <button
                type="button"
                className="w-full py-4 rounded-full bg-gradient-to-r from-purple-700 to-purple-500 hover:from-purple-600 hover:to-purple-400 transition text-white text-lg font-semibold shadow-lg shadow-purple-600/30"
              >
                LOGIN
              </button>
            </form>
          </div>

          {/* Bottom glow line */}
          <div className="absolute bottom-0 left-0 w-full h-[3px] bg-gradient-to-r from-transparent via-purple-500 to-transparent opacity-80" />
        </div>
      </div>
    </div>
  );
}
