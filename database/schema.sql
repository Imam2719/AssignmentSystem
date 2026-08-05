-- Assignment & Submission Management System — reference schema
-- This mirrors the EF Core model (see backend/src/AssignmentSystem.Api/Data/ApplicationDbContext.cs).
-- You do NOT need to run this manually if you use EF Core migrations (recommended, see README).
-- It's provided as a database script alternative / for quick inspection.

CREATE TABLE IF NOT EXISTS "SchoolClasses" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(300),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS "Users" (
    "Id" SERIAL PRIMARY KEY,
    "FullName" VARCHAR(150) NOT NULL,
    "Email" VARCHAR(200) NOT NULL UNIQUE,
    "PasswordHash" TEXT NOT NULL,
    "Role" VARCHAR(20) NOT NULL, -- Admin | Teacher | Student
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT now(),
    "SchoolClassId" INT REFERENCES "SchoolClasses"("Id") ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS "Subjects" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "Code" VARCHAR(20),
    "SchoolClassId" INT NOT NULL REFERENCES "SchoolClasses"("Id") ON DELETE CASCADE,
    UNIQUE ("SchoolClassId", "Name")
);

CREATE TABLE IF NOT EXISTS "TeacherSubjectAssignments" (
    "Id" SERIAL PRIMARY KEY,
    "TeacherId" INT NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    "SubjectId" INT NOT NULL REFERENCES "Subjects"("Id") ON DELETE CASCADE,
    "AssignedAt" TIMESTAMP NOT NULL DEFAULT now(),
    UNIQUE ("TeacherId", "SubjectId")
);

CREATE TABLE IF NOT EXISTS "Assignments" (
    "Id" SERIAL PRIMARY KEY,
    "Title" VARCHAR(200) NOT NULL,
    "Description" TEXT NOT NULL,
    "Deadline" TIMESTAMP NOT NULL,
    "MaxMarks" INT NOT NULL,
    "Status" VARCHAR(20) NOT NULL DEFAULT 'Draft', -- Draft | Published
    "AllowResubmission" BOOLEAN NOT NULL DEFAULT TRUE,
    "SchoolClassId" INT NOT NULL REFERENCES "SchoolClasses"("Id"),
    "SubjectId" INT NOT NULL REFERENCES "Subjects"("Id"),
    "CreatedByTeacherId" INT NOT NULL REFERENCES "Users"("Id"),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT now(),
    "UpdatedAt" TIMESTAMP
);

CREATE TABLE IF NOT EXISTS "Submissions" (
    "Id" SERIAL PRIMARY KEY,
    "AssignmentId" INT NOT NULL REFERENCES "Assignments"("Id") ON DELETE CASCADE,
    "StudentId" INT NOT NULL REFERENCES "Users"("Id"),
    "AnswerText" TEXT NOT NULL,
    "AttachmentUrl" TEXT,
    "Status" VARCHAR(30) NOT NULL DEFAULT 'Submitted',
    "SubmittedAt" TIMESTAMP NOT NULL DEFAULT now(),
    "UpdatedAt" TIMESTAMP,
    "MarksObtained" INT,
    "Feedback" TEXT,
    "GradedByTeacherId" INT REFERENCES "Users"("Id") ON DELETE SET NULL,
    "GradedAt" TIMESTAMP,
    UNIQUE ("AssignmentId", "StudentId")
);

CREATE INDEX IF NOT EXISTS idx_assignments_class ON "Assignments" ("SchoolClassId");
CREATE INDEX IF NOT EXISTS idx_submissions_assignment ON "Submissions" ("AssignmentId");
