"use client";

import { useEffect, useState } from "react";
import RoleGuard from "@/components/RoleGuard";
import Navbar from "@/components/Navbar";
import { api, Assignment, Submission } from "@/lib/api";

interface UserRow { id: number; fullName: string; email: string; role: string; isActive: boolean; schoolClassId?: number; schoolClassName?: string; }
interface ClassRow { id: number; name: string; description?: string; studentCount: number; subjectCount: number; }
interface SubjectRow { id: number; name: string; code?: string; schoolClassId: number; schoolClassName: string; }

const TABS = ["users", "classes", "subjects", "assign", "assignments"] as const;
type Tab = (typeof TABS)[number];
const TAB_LABEL: Record<Tab, string> = {
  users: "Users", classes: "Classes", subjects: "Subjects", assign: "Assign teacher", assignments: "Assignments",
};

function AdminDashboardContent() {
  const [tab, setTab] = useState<Tab>("users");
  const [users, setUsers] = useState<UserRow[]>([]);
  const [classes, setClasses] = useState<ClassRow[]>([]);
  const [subjects, setSubjects] = useState<SubjectRow[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const [userForm, setUserForm] = useState({ fullName: "", email: "", password: "", role: "Student", schoolClassId: "" as number | "" });
  const [classForm, setClassForm] = useState({ name: "", description: "" });
  const [subjectForm, setSubjectForm] = useState({ name: "", code: "", schoolClassId: "" as number | "" });
  const [assignForm, setAssignForm] = useState({ teacherId: "" as number | "", subjectId: "" as number | "" });

  // Assignments & submissions (admin: read-only view across the whole school)
  const [assignments, setAssignments] = useState<Assignment[]>([]);
  const [selectedAssignment, setSelectedAssignment] = useState<Assignment | null>(null);
  const [submissions, setSubmissions] = useState<Submission[]>([]);

  async function loadAll() {
    const [u, c, s, a] = await Promise.all([
      api.get<UserRow[]>("/users"),
      api.get<ClassRow[]>("/classes"),
      api.get<SubjectRow[]>("/classes/subjects"),
      api.get<Assignment[]>("/assignments"),
    ]);
    setUsers(u.data); setClasses(c.data); setSubjects(s.data); setAssignments(a.data);
    setLoading(false);
  }

  useEffect(() => { loadAll(); }, []);

  async function openSubmissions(a: Assignment) {
    if (selectedAssignment?.id === a.id) {
      setSelectedAssignment(null);
      return;
    }
    setSelectedAssignment(a);
    const { data } = await api.get<Submission[]>(`/submissions?assignmentId=${a.id}`);
    setSubmissions(data);
  }

  async function createUser(e: React.FormEvent) {
    e.preventDefault(); setError(null);
    if (userForm.role === "Student" && userForm.schoolClassId === "") {
      setError("Please select a class for the student.");
      return;
    }
    try {
      await api.post("/users", {
        ...userForm,
        schoolClassId: userForm.role === "Student" ? Number(userForm.schoolClassId) : null,
      });
      setUserForm({ fullName: "", email: "", password: "", role: "Student", schoolClassId: "" });
      await loadAll();
    } catch (e: any) { setError(e?.response?.data?.error || "Failed to create user."); }
  }

  async function createClass(e: React.FormEvent) {
    e.preventDefault(); setError(null);
    try {
      await api.post("/classes", classForm);
      setClassForm({ name: "", description: "" });
      await loadAll();
    } catch (e: any) { setError(e?.response?.data?.error || "Failed to create class."); }
  }

  async function createSubject(e: React.FormEvent) {
    e.preventDefault(); setError(null);
    if (subjectForm.schoolClassId === "") {
      setError("Please select a class for the subject.");
      return;
    }
    try {
      await api.post("/classes/subjects", { ...subjectForm, schoolClassId: Number(subjectForm.schoolClassId) });
      setSubjectForm({ name: "", code: "", schoolClassId: "" });
      await loadAll();
    } catch (e: any) { setError(e?.response?.data?.error || "Failed to create subject."); }
  }

  async function assignTeacher(e: React.FormEvent) {
    e.preventDefault(); setError(null);
    if (assignForm.teacherId === "" || assignForm.subjectId === "") {
      setError("Please select both a teacher and a subject.");
      return;
    }
    try {
      await api.post("/classes/assign-teacher", {
        teacherId: Number(assignForm.teacherId),
        subjectId: Number(assignForm.subjectId),
      });
      setAssignForm({ teacherId: "", subjectId: "" });
      await loadAll();
    } catch (e: any) { setError(e?.response?.data?.error || "Failed to assign teacher."); }
  }

  async function toggleActive(u: UserRow) {
    await api.put(`/users/${u.id}`, { isActive: !u.isActive });
    await loadAll();
  }

  const teachers = users.filter((u) => u.role === "Teacher");

  return (
    <div className="mx-auto max-w-5xl space-y-6 px-4 py-8 sm:px-6">
      <div className="rise-in flex flex-col justify-between gap-4 sm:flex-row sm:items-end">
        <div>
          <p className="label">Administration</p>
          <h1 className="font-display text-2xl font-semibold text-ink-900 sm:text-3xl">Admin Panel</h1>
        </div>
        <div className="flex flex-wrap gap-3">
          <div className="card min-w-[100px] px-4 py-2.5 text-center">
            <p className="font-mono-num text-xl font-semibold text-navy">{users.length}</p>
            <p className="label !text-[10px]">Users</p>
          </div>
          <div className="card min-w-[100px] px-4 py-2.5 text-center">
            <p className="font-mono-num text-xl font-semibold text-[var(--success)]">{classes.length}</p>
            <p className="label !text-[10px]">Classes</p>
          </div>
          <div className="card min-w-[100px] px-4 py-2.5 text-center">
            <p className="font-mono-num text-xl font-semibold text-brass-600">{subjects.length}</p>
            <p className="label !text-[10px]">Subjects</p>
          </div>
        </div>
      </div>

      <div className="segmented rise-in">
        {TABS.map((t) => (
          <button key={t} onClick={() => setTab(t)} data-active={tab === t}>
            {TAB_LABEL[t]}
          </button>
        ))}
      </div>

      {error && (
        <p className="rounded-md bg-[var(--danger-100)] px-3 py-2 text-sm font-medium text-[var(--danger)]">{error}</p>
      )}

      {tab === "users" && (
        <div className="rise-in space-y-4">
          <form onSubmit={createUser} className="card grid gap-3 p-5 sm:grid-cols-2">
            <div>
              <label className="label mb-1.5 block">Full name</label>
              <input required placeholder="Jane Doe" value={userForm.fullName}
                onChange={(e) => setUserForm({ ...userForm, fullName: e.target.value })} className="input" />
            </div>
            <div>
              <label className="label mb-1.5 block">Email</label>
              <input required type="email" placeholder="jane@school.test" value={userForm.email}
                onChange={(e) => setUserForm({ ...userForm, email: e.target.value })} className="input" />
            </div>
            <div>
              <label className="label mb-1.5 block">Password</label>
              <input required type="password" placeholder="••••••••" value={userForm.password}
                onChange={(e) => setUserForm({ ...userForm, password: e.target.value })} className="input" />
            </div>
            <div>
              <label className="label mb-1.5 block">Role</label>
              <select value={userForm.role} onChange={(e) => setUserForm({ ...userForm, role: e.target.value })} className="input">
                <option value="Student">Student</option>
                <option value="Teacher">Teacher</option>
                <option value="Admin">Admin</option>
              </select>
            </div>
            {userForm.role === "Student" && (
              <div className="sm:col-span-2">
                <label className="label mb-1.5 block">Class</label>
                <select required value={userForm.schoolClassId}
                  onChange={(e) => setUserForm({ ...userForm, schoolClassId: e.target.value === "" ? "" : Number(e.target.value) })} className="input">
                  <option value="">Select class</option>
                  {classes.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
                </select>
              </div>
            )}
            <button type="submit" className="btn btn-primary sm:col-span-2">Create user</button>
          </form>

          <div className="card overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full min-w-[560px] text-sm">
                <thead className="bg-navy-100 text-left">
                  <tr>
                    <th className="label px-4 py-3 font-semibold">Name</th>
                    <th className="label px-4 py-3 font-semibold">Email</th>
                    <th className="label px-4 py-3 font-semibold">Role</th>
                    <th className="label px-4 py-3 font-semibold">Class</th>
                    <th className="label px-4 py-3 font-semibold">Status</th>
                    <th className="label px-4 py-3 font-semibold"></th>
                  </tr>
                </thead>
                <tbody>
                  {users.map((u) => (
                    <tr key={u.id} className="border-t border-line">
                      <td className="px-4 py-3 font-medium text-ink-900">{u.fullName}</td>
                      <td className="px-4 py-3 text-ink-500">{u.email}</td>
                      <td className="px-4 py-3"><span className="badge badge-slate">{u.role}</span></td>
                      <td className="px-4 py-3 text-ink-500">{u.schoolClassName || "—"}</td>
                      <td className="px-4 py-3">
                        <span className={`badge ${u.isActive ? "badge-success" : "badge-danger"}`}>
                          {u.isActive ? "Active" : "Inactive"}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-right">
                        <button onClick={() => toggleActive(u)} className="btn btn-ghost !px-2 !py-1 !text-xs">
                          {u.isActive ? "Deactivate" : "Activate"}
                        </button>
                      </td>
                    </tr>
                  ))}
                  {!loading && users.length === 0 && (
                    <tr><td colSpan={6} className="px-4 py-8 text-center text-ink-500">No users yet.</td></tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {tab === "classes" && (
        <div className="rise-in space-y-4">
          <form onSubmit={createClass} className="card flex flex-col gap-3 p-5 sm:flex-row">
            <input required placeholder="Class name" value={classForm.name}
              onChange={(e) => setClassForm({ ...classForm, name: e.target.value })} className="input flex-1" />
            <input placeholder="Description" value={classForm.description}
              onChange={(e) => setClassForm({ ...classForm, description: e.target.value })} className="input flex-1" />
            <button type="submit" className="btn btn-primary">Add class</button>
          </form>
          <div className="grid gap-3 sm:grid-cols-2">
            {classes.map((c) => (
              <div key={c.id} className="card flex items-center justify-between p-4">
                <div>
                  <p className="font-display font-semibold text-ink-900">{c.name}</p>
                  {c.description && <p className="text-xs text-ink-500">{c.description}</p>}
                </div>
                <div className="flex gap-4 text-right">
                  <div>
                    <p className="font-mono-num text-lg font-semibold text-navy">{c.studentCount}</p>
                    <p className="label !text-[9px]">students</p>
                  </div>
                  <div>
                    <p className="font-mono-num text-lg font-semibold text-brass-600">{c.subjectCount}</p>
                    <p className="label !text-[9px]">subjects</p>
                  </div>
                </div>
              </div>
            ))}
            {!loading && classes.length === 0 && (
              <p className="card p-6 text-center text-sm text-ink-500 sm:col-span-2">No classes yet.</p>
            )}
          </div>
        </div>
      )}

      {tab === "subjects" && (
        <div className="rise-in space-y-4">
          <form onSubmit={createSubject} className="card flex flex-col gap-3 p-5 sm:flex-row">
            <input required placeholder="Subject name" value={subjectForm.name}
              onChange={(e) => setSubjectForm({ ...subjectForm, name: e.target.value })} className="input flex-1" />
            <input placeholder="Code" value={subjectForm.code}
              onChange={(e) => setSubjectForm({ ...subjectForm, code: e.target.value })} className="input sm:w-24" />
            <select required value={subjectForm.schoolClassId}
              onChange={(e) => setSubjectForm({ ...subjectForm, schoolClassId: e.target.value === "" ? "" : Number(e.target.value) })} className="input sm:w-44">
              <option value="">Select class</option>
              {classes.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
            <button type="submit" className="btn btn-primary">Add subject</button>
          </form>
          <div className="grid gap-3 sm:grid-cols-2">
            {subjects.map((s) => (
              <div key={s.id} className="card flex items-center justify-between p-4">
                <div>
                  <p className="font-display font-semibold text-ink-900">{s.name}</p>
                  <p className="text-xs text-ink-500">{s.schoolClassName}</p>
                </div>
                {s.code && <span className="badge badge-brass">{s.code}</span>}
              </div>
            ))}
            {!loading && subjects.length === 0 && (
              <p className="card p-6 text-center text-sm text-ink-500 sm:col-span-2">No subjects yet.</p>
            )}
          </div>
        </div>
      )}

      {tab === "assign" && (
        <form onSubmit={assignTeacher} className="card rise-in flex flex-col gap-3 p-5 sm:flex-row">
          <select required value={assignForm.teacherId}
            onChange={(e) => setAssignForm({ ...assignForm, teacherId: e.target.value === "" ? "" : Number(e.target.value) })} className="input flex-1">
            <option value="">Select teacher</option>
            {teachers.map((t) => <option key={t.id} value={t.id}>{t.fullName}</option>)}
          </select>
          <select required value={assignForm.subjectId}
            onChange={(e) => setAssignForm({ ...assignForm, subjectId: e.target.value === "" ? "" : Number(e.target.value) })} className="input flex-1">
            <option value="">Select subject</option>
            {subjects.map((s) => <option key={s.id} value={s.id}>{s.name} ({s.schoolClassName})</option>)}
          </select>
          <button type="submit" className="btn btn-primary">Assign</button>
        </form>
      )}

      {tab === "assignments" && (
        <div className="rise-in space-y-4">
          <p className="text-sm text-ink-500">
            Read-only view across every class and teacher. Grading happens on the teacher&apos;s dashboard.
          </p>

          {!loading && assignments.length === 0 && (
            <p className="card p-6 text-center text-sm text-ink-500">No assignments have been created yet.</p>
          )}

          <div className="grid gap-4">
            {assignments.map((a) => (
              <div key={a.id} className="card p-5">
                <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-start">
                  <div>
                    <h2 className="font-display text-lg font-semibold text-ink-900">{a.title}</h2>
                    <p className="mt-1 text-sm text-ink-500">
                      {a.schoolClassName} · {a.subjectName} · by {a.createdByTeacherName} · Due{" "}
                      {new Date(a.deadline).toLocaleString()}
                    </p>
                  </div>
                  <span className={`badge ${a.status === "Published" ? "badge-success" : "badge-slate"}`}>
                    {a.status}
                  </span>
                </div>

                <div className="mt-4 flex flex-wrap gap-2">
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
                      <div key={s.id} className="rounded-[10px] border border-line bg-paper/60 p-4">
                        <div className="flex items-start justify-between gap-3">
                          <div>
                            <p className="text-sm font-semibold text-ink-900">{s.studentName}</p>
                            <p className="mt-1 text-sm text-ink-700">{s.answerText}</p>
                          </div>
                          <span className="badge badge-slate shrink-0">{s.status}</span>
                        </div>
                        {s.status === "Graded" && (
                          <p className="mt-2 text-xs text-ink-500">
                            Marks: <span className="font-mono-num">{s.marksObtained}</span> / {a.maxMarks}
                            {s.feedback && <> · Feedback: {s.feedback}</>}
                          </p>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

export default function AdminPage() {
  return (
    <RoleGuard allowedRoles={["Admin"]}>
      <Navbar />
      <AdminDashboardContent />
    </RoleGuard>
  );
}
