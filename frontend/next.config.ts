import type { NextConfig } from "next";
import fs from "fs";
import path from "path";

// Load .env from the repo root (parent directory of frontend/)
const rootEnvPath = path.resolve(__dirname, "../.env");
const rootEnvVars: Record<string, string> = {};

if (fs.existsSync(rootEnvPath)) {
  const lines = fs.readFileSync(rootEnvPath, "utf-8").split("\n");
  for (const line of lines) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith("#")) continue;
    const eqIndex = trimmed.indexOf("=");
    if (eqIndex === -1) continue;
    const key = trimmed.slice(0, eqIndex).trim();
    const value = trimmed.slice(eqIndex + 1).trim();
    rootEnvVars[key] = value;
  }
}

const nextConfig: NextConfig = {
  env: rootEnvVars,
};

export default nextConfig;