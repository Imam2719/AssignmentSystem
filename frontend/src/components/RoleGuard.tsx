"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthContext";
import { Role } from "@/lib/api";

export default function RoleGuard({
  allowedRoles,
  children,
}: {
  allowedRoles: Role[];
  children: React.ReactNode;
}) {
  const { user, loading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (loading) return;
    if (!user) {
      router.push("/login");
      return;
    }
    if (!allowedRoles.includes(user.role)) {
      router.push("/login");
    }
  }, [user, loading, allowedRoles, router]);

  if (loading || !user || !allowedRoles.includes(user.role)) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-paper">
        <div className="flex flex-col items-center gap-3">
          <span className="flex h-10 w-10 animate-pulse items-center justify-center rounded-md bg-navy font-display text-lg font-bold text-brass">
            L
          </span>
          <p className="text-sm text-ink-500">Checking your access…</p>
        </div>
      </div>
    );
  }

  return <>{children}</>;
}
