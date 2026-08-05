"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthContext";

export default function Home() {
  const { user, loading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (loading) return;
    if (!user) {
      router.push("/login");
      return;
    }
    const dest =
      user.role === "Admin" ? "/admin" : user.role === "Teacher" ? "/teacher" : "/student";
    router.push(dest);
  }, [user, loading, router]);

  return (
    <div className="flex min-h-screen items-center justify-center bg-paper">
      <div className="flex flex-col items-center gap-3">
        <span className="flex h-10 w-10 animate-pulse items-center justify-center rounded-md bg-navy font-display text-lg font-bold text-brass">
          L
        </span>
        <p className="text-sm text-ink-500">Opening your register…</p>
      </div>
    </div>
  );
}
