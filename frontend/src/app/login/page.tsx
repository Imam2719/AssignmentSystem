"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useAuth } from "@/context/AuthContext";

const schema = z.object({
  email: z.string().email("Enter a valid email address"),
  password: z.string().min(1, "Password is required"),
});

type FormData = z.infer<typeof schema>;

const DEMO_ACCOUNTS = [
  { role: "Admin", email: "admin@school.test", password: "12345" },
  { role: "Teacher", email: "teacher@school.test", password: "12345" },
  { role: "Student", email: "student@school.test", password: "12345" },
];

export default function LoginPage() {
  const { login } = useAuth();
  const [serverError, setServerError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm<FormData>({ resolver: zodResolver(schema) });

  async function onSubmit(data: FormData) {
    setServerError(null);
    setSubmitting(true);
    try {
      await login(data.email, data.password);
    } catch (e: any) {
      if (e?.response?.status === 401) {
        setServerError("Invalid email or password.");
      } else if (e?.response) {
        setServerError(e.response.data?.error || e.response.data?.message || `Server error (${e.response.status}).`);
      } else if (e?.request) {
        setServerError("Could not reach the server. Is the backend running and is NEXT_PUBLIC_API_BASE_URL correct?");
      } else {
        setServerError("Something went wrong. Please try again.");
      }
    } finally {
      setSubmitting(false);
    }
  }

  function fillDemo(email: string, password: string) {
    setValue("email", email, { shouldValidate: true });
    setValue("password", password, { shouldValidate: true });
  }

  return (
    <div className="flex min-h-screen">
      {/* Brand panel — hidden on small screens */}
      <div className="relative hidden w-[42%] flex-col justify-between overflow-hidden bg-navy px-10 py-12 text-white lg:flex">
        <div
          className="pointer-events-none absolute inset-0 opacity-[0.06]"
          style={{
            backgroundImage:
              "repeating-linear-gradient(0deg, #fff 0, #fff 1px, transparent 1px, transparent 28px), repeating-linear-gradient(90deg, #fff 0, #fff 1px, transparent 1px, transparent 28px)",
          }}
        />
        <div className="relative">
          <span className="flex h-10 w-10 items-center justify-center rounded-md bg-brass font-display text-lg font-bold text-[#241705]">
            L
          </span>
          <p className="mt-6 font-display text-3xl font-semibold leading-tight">
            Every assignment,
            <br />
            every submission,
            <br />
            on the record.
          </p>
          <p className="mt-4 max-w-sm text-sm text-white/60">
            A single register for admins, teachers, and students to publish work,
            submit answers, and settle marks — with a full audit trail.
          </p>
        </div>

        <div className="relative flex items-center gap-6 text-xs uppercase tracking-[0.14em] text-white/40">
          <span>Admin</span>
          <span className="h-1 w-1 rounded-full bg-white/30" />
          <span>Teacher</span>
          <span className="h-1 w-1 rounded-full bg-white/30" />
          <span>Student</span>
        </div>
      </div>

      {/* Form panel */}
      <div className="flex flex-1 items-center justify-center px-4 py-10 sm:px-6">
        <div className="w-full max-w-sm">
          <div className="mb-6 lg:hidden">
            <span className="flex h-9 w-9 items-center justify-center rounded-md bg-navy font-display text-base font-bold text-brass">
              L
            </span>
          </div>

          <h1 className="font-display text-2xl font-semibold text-ink-900">Welcome back</h1>
          <p className="mb-7 mt-1 text-sm text-ink-500">Sign in to open your register.</p>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
            <div>
              <label className="label mb-1.5 block">Email</label>
              <input
                {...register("email")}
                type="email"
                autoComplete="email"
                className="input"
                placeholder="you@school.test"
              />
              {errors.email && <p className="mt-1 text-xs text-[var(--danger)]">{errors.email.message}</p>}
            </div>

            <div>
              <label className="label mb-1.5 block">Password</label>
              <input
                {...register("password")}
                type="password"
                autoComplete="current-password"
                className="input"
                placeholder="••••••••"
              />
              {errors.password && (
                <p className="mt-1 text-xs text-[var(--danger)]">{errors.password.message}</p>
              )}
            </div>

            {serverError && (
              <p className="rounded-md bg-[var(--danger-100)] px-3 py-2 text-xs font-medium text-[var(--danger)]">
                {serverError}
              </p>
            )}

            <button type="submit" disabled={submitting} className="btn btn-primary w-full">
              {submitting ? "Signing in…" : "Sign in"}
            </button>
          </form>

          <div className="mt-8 rounded-[14px] border border-line bg-white/60 p-4">
            <p className="label mb-3">Demo accounts</p>
            <div className="space-y-2">
              {DEMO_ACCOUNTS.map((acc) => (
                <button
                  key={acc.role}
                  type="button"
                  onClick={() => fillDemo(acc.email, acc.password)}
                  className="flex w-full items-center justify-between rounded-md border border-line px-3 py-2 text-left text-xs transition hover:border-navy hover:bg-navy-100"
                >
                  <span className="font-semibold text-ink-700">{acc.role}</span>
                  <span className="font-mono-num text-ink-500">{acc.email}</span>
                </button>
              ))}
            </div>
            <p className="mt-2 text-[11px] text-ink-300">Tap a role to autofill the form.</p>
          </div>
        </div>
      </div>
    </div>
  );
}
