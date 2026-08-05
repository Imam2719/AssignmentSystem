"use client";

import { useEffect, useState } from "react";
import RoleGuard from "@/components/RoleGuard";
import Navbar from "@/components/Navbar";
import { api, Assignment, Submission } from "@/lib/api";

interface ClassOption { id: number; name: string; }
interface SubjectOption { id: number; name: string; schoolClassId: number; }

function TeacherDashboardContent() {
  const [assignments, setAssignments] = useState<Assignment[]>([]);
  const [classes, setClasses] = useState<ClassOption[]>([]);
  const [subjects, setSubjects] = useState<SubjectOption[]>([]);
  const [showForm, setShowForm] = useState(false);
  const [selectedAssignment, setSelectedAssignment] = useState<Assignment | null>(null);
  const [submissions, setSubmissions] = useState<Submission[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const [form, setForm] = useState({
    title: "", description: "", deadline: "", maxMarks: 100,
    schoolClassId: "" as number | "", subjectId: "" as number | "", publishNow: false,
  });

  async function loadAll() {
    const [a, c, s] = await Promise.all([
      api.get<Assignment[]>("/assignments"),
      api.get<ClassOption[]>("/classes"),
      // Only the subjects THIS teacher is assigned to teach — not every subject in the school.
      api.get<SubjectOption[]>("/classes/subjects/mine"),
    ]);
    setAssignments(a.data);
    setClasses(c.data);
    setSubjects(s.data);
    setLoading(false);
  }

  useEffect(() => { loadAll(); }, []);

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    setError(null);

    if (form.schoolClassId === "" || form.subjectId === "") {
      setError("Please select both a class and a subject.");
      return;
    }

    try {
      await api.post("/assignments", {
        ...form,
        schoolClassId: Number(form.schoolClassId),
        subjectId: Number(form.subjectId),
        deadline: new Date(form.deadline).toISOString(),
        allowResubmission: true,
      });
      setShowForm(false);
      setForm({ title: "", description: "", deadline: "", maxMarks: 100, schoolClassId: "", subjectId: "", publishNow: false });
      await loadAll();
    } catch (e: any) {
      setError(e?.response?.data?.error || "Failed to create assignment.");
    }
  }

  async function togglePublish(a: Assignment) {
    const newStatus = a.status === "Draft" ? "Published" : "Draft";
    await api.patch(`/assignments/${a.id}/status`, JSON.stringify(newStatus), {
      headers: { "Content-Type": "application/json" },
    });
    await loadAll();
  }

  async function openSubmissions(a: Assignment) {
    if (selectedAssignment?.id === a.id) {
      setSelectedAssignment(null);
      return;
    }
    setSelectedAssignment(a);
    const { data } = await api.get<Submission[]>(`/submissions?assignmentId=${a.id}`);
    setSubmissions(data);
  }

  async function grade(submissionId: number, marks: number, feedback: string) {
    await api.post(`/submissions/${submissionId}/grade`, { marksObtained: marks, feedback });
    if (selectedAssignment) {
      const { data } = await api.get<Submission[]>(`/submissions?assignmentId=${selectedAssignment.id}`);
      setSubmissions(data);
      await loadAll();
    }
  }

  const availableSubjects = subjects.filter((s) => s.schoolClassId === form.schoolClassId);
  // ^ form.schoolClassId is "" until a class is picked, so this naturally stays empty until then.
  const published = assignments.filter((a) => a.status === "Published").length;
  const totalSubmissions = assignments.reduce((sum, a) => sum + a.submissionCount, 0);

  return (
    <div className="mx-auto max-w-5xl space-y-6 px-4 py-8 sm:px-6">
      <div className="rise-in flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
        <div>
          <p className="label">Your register</p>
          <h1 className="font-display text-2xl font-semibold text-ink-900 sm:text-3xl">My Assignments</h1>
        </div>
        <div className="flex flex-wrap gap-3">
          <div className="card min-w-[110px] px-4 py-2.5 text-center">
            <p className="font-mono-num text-xl font-semibold text-navy">{assignments.length}</p>
            <p className="label !text-[10px]">Total</p>
          </div>
          <div className="card min-w-[110px] px-4 py-2.5 text-center">
            <p className="font-mono-num text-xl font-semibold text-[var(--success)]">{published}</p>
            <p className="label !text-[10px]">Published</p>
          </div>
          <div className="card min-w-[110px] px-4 py-2.5 text-center">
            <p className="font-mono-num text-xl font-semibold text-brass-600">{totalSubmissions}</p>
            <p className="label !text-[10px]">Submissions</p>
          </div>
          <button onClick={() => setShowForm(!showForm)} className="btn btn-brass">
            {showForm ? "Cancel" : "+ New assignment"}
          </button>
        </div>
      </div>

      {showForm && (
        <form onSubmit={handleCreate} className="card rise-in space-y-4 p-5">
          <div>
            <label className="label mb-1.5 block">Title</label>
            <input required placeholder="e.g. Algebra Basics — Worksheet 2" value={form.title}
              onChange={(e) => setForm({ ...form, title: e.target.value })} className="input" />
          </div>
          <div>
            <label className="label mb-1.5 block">Description</label>
            <textarea required placeholder="What should students do?" value={form.description}
              onChange={(e) => setForm({ ...form, description: e.target.value })} className="input" rows={3} />
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <label className="label mb-1.5 block">Deadline</label>
              <input required type="datetime-local" value={form.deadline}
                onChange={(e) => setForm({ ...form, deadline: e.target.value })} className="input" />
            </div>
            <div>
              <label className="label mb-1.5 block">Max marks</label>
              <input required type="number" min={1} value={form.maxMarks}
                onChange={(e) => setForm({ ...form, maxMarks: Number(e.target.value) })} className="input" />
            </div>
            <div>
              <label className="label mb-1.5 block">Class</label>
              <select required value={form.schoolClassId}
                onChange={(e) => setForm({ ...form, schoolClassId: e.target.value === "" ? "" : Number(e.target.value), subjectId: "" })}
                className="input">
                <option value="">Select class</option>
                {classes.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </div>
            <div>
              <label className="label mb-1.5 block">Subject</label>
              <select required value={form.subjectId} disabled={form.schoolClassId === ""}
                onChange={(e) => setForm({ ...form, subjectId: e.target.value === "" ? "" : Number(e.target.value) })} className="input">
                <option value="">
                  {form.schoolClassId === "" ? "Select a class first" : "Select subject"}
                </option>
                {availableSubjects.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select>
              {form.schoolClassId !== "" && availableSubjects.length === 0 && (
                <p className="mt-1 text-xs text-[var(--danger)]">
                  You aren&apos;t assigned to teach any subject in this class yet — ask an Admin to assign one.
                </p>
              )}
            </div>
          </div>
          <label className="flex items-center gap-2 text-sm text-ink-700">
            <input type="checkbox" checked={form.publishNow}
              onChange={(e) => setForm({ ...form, publishNow: e.target.checked })} />
            Publish immediately
          </label>
          {error && <p className="rounded-md bg-[var(--danger-100)] px-3 py-2 text-xs font-medium text-[var(--danger)]">{error}</p>}
          <button type="submit" className="btn btn-primary">Create assignment</button>
        </form>
      )}

      {!loading && assignments.length === 0 && (
        <div className="card rise-in p-10 text-center">
          <p className="font-display text-lg text-ink-700">No assignments yet</p>
          <p className="mt-1 text-sm text-ink-500">Create your first assignment to get started.</p>
        </div>
      )}

      <div className="grid gap-4">
        {assignments.map((a, i) => (
          <div key={a.id} className="card rise-in p-5" style={{ animationDelay: `${Math.min(i, 6) * 40}ms` }}>
            <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-start">
              <div>
                <h2 className="font-display text-lg font-semibold text-ink-900">{a.title}</h2>
                <p className="mt-1 text-sm text-ink-500">
                  {a.schoolClassName} · {a.subjectName} · Due {new Date(a.deadline).toLocaleString()}
                </p>
              </div>
              <span className={`badge ${a.status === "Published" ? "badge-success" : "badge-slate"}`}>
                {a.status}
              </span>
            </div>

            <div className="mt-4 flex flex-wrap gap-2">
              <button onClick={() => togglePublish(a)} className="btn btn-outline">
                {a.status === "Draft" ? "Publish" : "Unpublish"}
              </button>
              <button onClick={() => openSubmissions(a)} className="btn btn-outline">
                {selectedAssignment?.id === a.id ? "Hide submissions" : `View submissions (${a.submissionCount})`}
              </button>
            </div>

            {selectedAssignment?.id === a.id && (
              <div className="mt-4 space-y-3 border-t border-line pt-4">
                {submissions.length === 0 && (
                  <p className="text-sm text-ink-500">No submissions yet.</p>
                )}
                {submissions.map((s) => (
                  <SubmissionRow key={s.id} submission={s} maxMarks={a.maxMarks} onGrade={grade} />
                ))}
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}

function SubmissionRow({ submission, maxMarks, onGrade }: {
  submission: Submission; maxMarks: number; onGrade: (id: number, marks: number, feedback: string) => void;
}) {
  const [marks, setMarks] = useState(submission.marksObtained ?? 0);
  const [feedback, setFeedback] = useState(submission.feedback ?? "");
  const graded = submission.status === "Graded";

  return (
    <div className="rounded-[10px] border border-line bg-paper/60 p-4">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-sm font-semibold text-ink-900">{submission.studentName}</p>
          <p className="mt-1 text-sm text-ink-700">{submission.answerText}</p>
        </div>
        {graded && (
          <div className="seal shrink-0" style={{ width: "3.4rem", height: "3.4rem" }}>
            <span className="font-mono-num text-sm font-bold leading-none">{submission.marksObtained}</span>
            <span className="text-[8px] font-semibold leading-none">/ {maxMarks}</span>
          </div>
        )}
      </div>
      <div className="mt-3 flex flex-wrap items-center gap-2">
        <input type="number" min={0} max={maxMarks} value={marks}
          onChange={(e) => setMarks(Number(e.target.value))}
          className="input !w-20" />
        <span className="text-sm text-ink-500">/ {maxMarks}</span>
        <input placeholder="Feedback" value={feedback} onChange={(e) => setFeedback(e.target.value)}
          className="input flex-1 !min-w-[140px]" />
        <button onClick={() => onGrade(submission.id, marks, feedback)} className="btn btn-primary">
          {graded ? "Update grade" : "Grade"}
        </button>
      </div>
    </div>
  );
}

export default function TeacherPage() {
  return (
    <RoleGuard allowedRoles={["Teacher"]}>
      <Navbar />
      <TeacherDashboardContent />
    </RoleGuard>
  );
}
