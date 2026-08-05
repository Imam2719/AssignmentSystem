import axios from "axios";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5000/api";

export const api = axios.create({
  baseURL: API_BASE_URL,
  headers: { "Content-Type": "application/json" },
});

// Attach JWT token (from localStorage) to every request
api.interceptors.request.use((config) => {
  if (typeof window !== "undefined") {
    const token = localStorage.getItem("token");
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
  }
  return config;
});

// Redirect to /login on 401 (expired/invalid token)
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401 && typeof window !== "undefined") {
      localStorage.removeItem("token");
      localStorage.removeItem("user");
      window.location.href = "/login";
    }
    return Promise.reject(error);
  }
);

export type Role = "Admin" | "Teacher" | "Student";

export interface AuthUser {
  userId: number;
  fullName: string;
  email: string;
  role: Role;
}

export interface Assignment {
  id: number;
  title: string;
  description: string;
  deadline: string;
  maxMarks: number;
  status: "Draft" | "Published";
  allowResubmission: boolean;
  schoolClassId: number;
  schoolClassName: string;
  subjectId: number;
  subjectName: string;
  createdByTeacherId: number;
  createdByTeacherName: string;
  createdAt: string;
  isPastDeadline: boolean;
  submissionCount: number;
}

export interface Submission {
  id: number;
  assignmentId: number;
  assignmentTitle: string;
  maxMarks: number;
  studentId: number;
  studentName: string;
  answerText: string;
  attachmentUrl?: string | null;
  status: string;
  submittedAt: string;
  updatedAt?: string | null;
  marksObtained?: number | null;
  feedback?: string | null;
  gradedAt?: string | null;
}
