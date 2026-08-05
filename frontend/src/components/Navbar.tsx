"use client";

import { useState } from "react";
import { useAuth } from "@/context/AuthContext";

const ROLE_STYLE: Record<string, string> = {
  Admin: "badge-brass",
  Teacher: "badge-info",
  Student: "badge-success",
};

function initials(name: string) {
  return name
    .split(" ")
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0]?.toUpperCase())
    .join("");
}

export default function Navbar() {
  const { user, logout } = useAuth();
  const [menuOpen, setMenuOpen] = useState(false);

  return (
    <header className="sticky top-0 z-40 border-b border-line bg-navy text-white">
      <div className="mx-auto flex max-w-6xl items-center justify-between gap-3 px-4 py-3 sm:px-6">
        <a href="/" className="flex items-center gap-2.5">
          <span className="flex h-8 w-8 items-center justify-center rounded-md bg-brass text-sm font-bold text-[#241705] font-display">
            L
          </span>
          <div className="leading-tight">
            <p className="font-display text-[15px] font-semibold tracking-tight">Ledger</p>
            <p className="hidden text-[10px] uppercase tracking-[0.14em] text-white/50 sm:block">
              Assignment &amp; Submission Registry
            </p>
          </div>
        </a>

        {user && (
          <div className="flex items-center gap-3">
            <div className="hidden items-center gap-3 sm:flex">
              <span className={`badge ${ROLE_STYLE[user.role] ?? "badge-slate"}`}>{user.role}</span>
              <div className="flex items-center gap-2 rounded-full bg-white/5 py-1 pl-1 pr-3">
                <span className="flex h-7 w-7 items-center justify-center rounded-full bg-white/15 text-xs font-semibold">
                  {initials(user.fullName)}
                </span>
                <span className="text-sm text-white/90">{user.fullName}</span>
              </div>
              <button onClick={logout} className="btn btn-brass !py-1.5 !px-3 !text-xs">
                Log out
              </button>
            </div>

            <button
              onClick={() => setMenuOpen((v) => !v)}
              className="flex h-9 w-9 items-center justify-center rounded-md border border-white/15 text-white sm:hidden"
              aria-label="Toggle menu"
              aria-expanded={menuOpen}
            >
              <span className="flex h-7 w-7 items-center justify-center rounded-full bg-white/15 text-xs font-semibold">
                {initials(user.fullName)}
              </span>
            </button>
          </div>
        )}
      </div>

      {user && menuOpen && (
        <div className="border-t border-white/10 bg-navy-700 px-4 py-3 sm:hidden">
          <p className="text-sm font-medium">{user.fullName}</p>
          <span className={`badge ${ROLE_STYLE[user.role] ?? "badge-slate"} mt-2`}>{user.role}</span>
          <button onClick={logout} className="btn btn-brass mt-3 w-full !text-xs">
            Log out
          </button>
        </div>
      )}
    </header>
  );
}
