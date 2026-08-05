"use client";

import { useEffect, useState } from "react";
import RoleGuard from "@/components/RoleGuard";
import Navbar from "@/components/Navbar";
import { api, Assignment, Submission } from "@/lib/api";

function statusBadge(sub: Submission | undefined, isPastDeadline: boolean) {
  if (sub?.status === "Graded") return { label: "Graded", cls: "badge-brass" };
  if (sub) return { label: sub.status, cls: "badge-info" };
  if (isPastDeadline) return { label: "Deadline passed", cls: "badge-danger" };
  return { label: "Not submitted", cls: "badge-slate" };
}

function StudentDashboardContent() {
  const [assignments, setAssignments] = useState<Assignment[]>([]);
  const [mySubmissions, setMySubmissions] = useState<Submission[]>([]);
  const [selected, setSelected] = useState<Assignment | null>(null);
  const [answerText, setAnswerText] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  async function loadData() {
    setLoading(true);
    const [a, s] = await Promise.all([
      api.get<Assignment[]>("/assignments"),
      api.get<Submission[]>("/submissions"),
    ]);
    setAssignments(a.data);
    setMySubmissions(s.data);
    setLoading(false);
  }

  useEffect(() => {
    loadData();
  }, []);

  function submissionFor(assignmentId: number) {
    return mySubmissions.find((s) => s.assignmentId === assignmentId);
  }

  async function handleSubmit(assignmentId: number) {
    setError(null);
    try {
      const existing = submissionFor(assignmentId);
      if (existing) {
        await api.put(`/submissions/${existing.id}`, { answerText, attachmentUrl: null });
      } else {
        await api.post(`/submissions/assignments/${assignmentId}`, { answerText, attachmentUrl: null });
      }
      setSelected(null);
      setAnswerText("");
      await loadData();
    } catch (e: any) {
      setError(e?.response?.data?.error || "Failed to submit.");
    }
  }

  const pending = assignments.filter((a) => !submissionFor(a.id) && !a.isPastDeadline).length;
  const graded = mySubmissions.filter((s) => s.status === "Graded").length;

  if (loading) {
    return (
      <div className="mx-auto max-w-5xl space-y-4 px-4 py-8 sm:px-6">
        <div className="h-24 animate-pulse rounded-2xl bg-white/60" />
        <div className="h-40 animate-pulse rounded-2xl bg-white/60" />
        <div className="h-40 animate-pulse rounded-2xl bg-white/60" />
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-5xl space-y-6 px-4 py-8 sm:px-6">
      <div className="rise-in flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
        <div>
          <p className="label">Your register</p>
          <h1 className="font-display text-2xl font-semibold text-ink-900 sm:text-3xl">My Assignments</h1>
        </div>
        <div className="flex gap-3">
          <div className="card min-w-[110px] px-4 py-2.5 text-center">
            <p className="font-mono-num text-xl font-semibold text-navy">{pending}</p>
            <p className="label !text-[10px]">Pending</p>
          </div>
          <div className="card min-w-[110px] px-4 py-2.5 text-center">
            <p className="font-mono-num text-xl font-semibold text-brass-600">{graded}</p>
            <p className="label !text-[10px]">Graded</p>
          </div>
        </div>
      </div>

      {assignments.length === 0 && (
        <div className="card rise-in p-10 text-center">
          <p className="font-display text-lg text-ink-700">Nothing here yet</p>
          <p className="mt-1 text-sm text-ink-500">
            Your teacher hasn&apos;t published any assignments for your class yet. Check back soon.
          </p>
        </div>
      )}

      <div className="grid gap-4">
        {assignments.map((a, i) => {
          const sub = submissionFor(a.id);
          const badge = statusBadge(sub, a.isPastDeadline);
          const canAct = !a.isPastDeadline && (!sub || (a.allowResubmission && sub.status !== "Graded"));

          return (
            <div
              key={a.id}
              className="card rise-in p-5"
              style={{ animationDelay: `${Math.min(i, 6) * 40}ms` }}
            >
              <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-start">
                <div>
                  <h2 className="font-display text-lg font-semibold text-ink-900">{a.title}</h2>
                  <p className="mt-1 text-sm text-ink-500">
                    {a.subjectName} · Due {new Date(a.deadline).toLocaleString()} · Max marks{" "}
                    <span className="font-mono-num">{a.maxMarks}</span>
                  </p>
                </div>
                <span className={`badge ${badge.cls}`}>{badge.label}</span>
              </div>

              <p className="mt-3 text-sm leading-relaxed text-ink-700">{a.description}</p>

              {sub && (
                <div className="mt-4 rounded-[10px] border border-line bg-paper/60 p-4">
                  <p className="label mb-1.5">Your answer</p>
                  <p className="text-sm text-ink-700">{sub.answerText}</p>
                  {sub.status === "Graded" && (
                    <div className="mt-4 flex items-center gap-4 border-t border-line pt-4">
                      <div className="seal">
                        <span className="font-mono-num text-lg font-bold leading-none">{sub.marksObtained}</span>
                        <span className="text-[9px] font-semibold leading-none">/ {a.maxMarks}</span>
                      </div>
                      <div>
                        <p className="label mb-1">Feedback</p>
                        <p className="text-sm text-ink-700">{sub.feedback || "No written feedback provided."}</p>
                      </div>
                    </div>
                  )}
                </div>
              )}

              {canAct && (
                <div className="mt-4">
                  {selected?.id === a.id ? (
                    <div className="space-y-3">
                      <textarea
                        value={answerText}
                        onChange={(e) => setAnswerText(e.target.value)}
                        rows={4}
                        className="input"
                        placeholder="Write your answer…"
                      />
                      {error && <p className="text-xs text-[var(--danger)]">{error}</p>}
                      <div className="flex gap-2">
                        <button onClick={() => handleSubmit(a.id)} className="btn btn-primary">
                          {sub ? "Update submission" : "Submit"}
                        </button>
                        <button onClick={() => setSelected(null)} className="btn btn-outline">
                          Cancel
                        </button>
                      </div>
                    </div>
                  ) : (
                    <button
                      onClick={() => {
                        setSelected(a);
                        setAnswerText(sub?.answerText || "");
                      }}
                      className="btn btn-outline"
                    >
                      {sub ? "Edit submission" : "Submit answer"}
                    </button>
                  )}
                </div>
              )}
            </div>
          );
        })}
      </div>
    </div>
  );
}

export default function StudentPage() {
  return (
    <RoleGuard allowedRoles={["Student"]}>
      <Navbar />
      <StudentDashboardContent />
    </RoleGuard>
  );
}
