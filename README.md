# Assignment & Submission Management System

A role-based (Admin / Teacher / Student) full-stack web app for a school/college to manage assignments and submissions.
<img width="1280" height="714" alt="image" src="https://github.com/user-attachments/assets/2d0ac22e-bbe4-45db-9fbf-7a66992c1b0b" />

## 1. Overview

Teachers create assignments for a specific class and subject, students submit answers before the deadline, and teachers review submissions and give marks + feedback. Admins manage users, classes, subjects, and teacher-subject assignments.

## 2. Main Features

**Admin**
- Create/deactivate users (Admin, Teacher, Student)
- Manage classes/courses and subjects
- Assign teachers to subjects
- View all data across the system

**Teacher**
- Create / edit / delete assignments (only for subjects they're assigned to)
- Publish or keep as draft
- View submissions for their assignments
- Grade submissions (marks + feedback), change submission status

**Student**
- View published assignments for their own class
- Submit an answer before the deadline
- Update submission before the deadline (if the assignment allows resubmission)
- View marks and feedback once graded

## 3. Technology Stack

| Layer | Tech |
|---|---|
| Frontend | Next.js 16 (App Router), React 19, TypeScript, Tailwind CSS, react-hook-form + zod, axios |
| Backend | ASP.NET Core 8 Web API, C#, EF Core, Swagger/OpenAPI, Serilog |
| Database | PostgreSQL (via Npgsql EF Core provider) |
| Auth | JWT Bearer tokens, ASP.NET Core role-based `[Authorize]` |
| Testing | xUnit + FluentAssertions + EF Core InMemory provider |

## 4. Project Structure

```
assignment-system/
├── backend/
│   ├── AssignmentSystem.sln
│   ├── src/AssignmentSystem.Api/
│   │   ├── Controllers/      # Auth, Users, Classes, Assignments, Submissions
│   │   ├── Models/           # EF Core entities
│   │   ├── DTOs/              # Request/response records
│   │   ├── Data/               # DbContext + DbInitializer (seed)
│   │   ├── Services/           # JWT TokenService
│   │   ├── Middleware/         # Global exception handling
│   │   └── Program.cs
│   └── tests/AssignmentSystem.Tests/   # xUnit business-rule tests
├── frontend/                 # Next.js app (App Router)
│   └── src/
│       ├── app/{login,admin,teacher,student}/page.tsx
│       ├── context/AuthContext.tsx
│       ├── components/{Navbar,RoleGuard}.tsx
│       └── lib/api.ts
├── database/
│   ├── schema.sql             # reference schema (optional, see below)
│   └── seed.sql
└── docker-compose.yml         # PostgreSQL container
```

## 5. Setup Instructions

### Prerequisites
- Node.js 20+
- .NET SDK 8
- PostgreSQL 16 (or Docker)
- Git

### 5.1 Database

**Option A — Docker (recommended):**
```bash
cd assignment-system
docker compose up -d
```
This starts PostgreSQL on `localhost:5432` with database `assignment_system` (user `postgres` / password `postgres`).

**Option B — local PostgreSQL install:** create a database named `assignment_system` yourself and update the connection string in step 5.2.

### 5.2 Backend

```bash
cd backend/src/AssignmentSystem.Api

# Configure secrets (don't commit real secrets — see .env.example)
# Easiest for local dev: just edit appsettings.Development.json / appsettings.json directly,
# or use dotnet user-secrets:
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "replace_with_a_long_random_secret_min_32_chars"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=assignment_system;Username=postgres;Password=postgres"

# Restore & build
dotnet restore
dotnet build

# Create the database schema via EF Core migrations (generates Migrations/ folder)
dotnet tool install --global dotnet-ef   # first time only
dotnet ef migrations add InitialCreate
dotnet ef database update

# Run the API
dotnet run
```
The API starts at `https://localhost:5001` (or `http://localhost:5000`) and seeds demo data automatically on first run (see `Data/DbInitializer.cs`). Swagger UI is available at `/swagger` in Development mode.

> If you'd rather not use EF migrations, `database/schema.sql` contains an equivalent hand-written schema you can run manually with `psql`. The app's built-in seeding (`DbInitializer`) still needs to run once via `dotnet run` to create the demo user accounts, since their passwords are hashed with ASP.NET Core's `PasswordHasher`.

### 5.3 Frontend

```bash
cd frontend
cp .env.example .env.local   # already present with sensible defaults
npm install
npm run dev
```
App runs at `http://localhost:3000` and expects the API at `http://localhost:5000/api` (see `NEXT_PUBLIC_API_BASE_URL`).

### 5.4 Running Tests

```bash
cd backend
dotnet test
```
Covers deadline enforcement, one-submission-per-student, marks-cannot-exceed-max, and cross-role/cross-class authorization checks.

## 6. Demo Credentials

| Role | Email | Password |
|---|---|---|
| Admin | admin@school.test | 12345 |
| Teacher | teacher@school.test | 12345 |
| Student | student@school.test | 12345 |

## 7. Assumptions

- A "class/course" and "subject" are separate entities; a subject always belongs to exactly one class (e.g. "Math" for Class 9 and "Math" for Class 10 are different rows), matching how school timetables usually work.
- A student submission is a single text answer plus an optional attachment URL (no file upload server was built, to keep scope reasonable — `AttachmentUrl` is a placeholder for wiring up file storage such as S3 later).
- A student can only have **one** submission per assignment; "updating a submission" is a PUT to the same row, not a new submission record.
- Only Admin can create user accounts (no public self-registration), matching a real school's onboarding process.
- A teacher can only create/manage assignments for subjects they've been explicitly assigned to by an Admin.
- JWT tokens are stored in `localStorage` on the frontend for simplicity; a production system would likely use httpOnly cookies.

## 8. Known Limitations

- No file upload for submissions/assignment attachments (only a URL field).
- No pagination/filtering on list endpoints yet (fine at demo scale, would need to be added for large classes).
- No email notifications for new assignments or grades.
- No password reset flow (Admin must reset via user update, not implemented).
- `.env.example` files are provided; the committed `appsettings.json` has placeholder (non-secret) local values for convenience — replace `Jwt:Key` before any real deployment.
