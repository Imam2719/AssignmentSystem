namespace AssignmentSystem.Api.Models;

public enum UserRole
{
    Admin = 0,
    Teacher = 1,
    Student = 2
}

public enum AssignmentStatus
{
    Draft = 0,
    Published = 1
}

public enum SubmissionStatus
{
    Submitted = 0,      // student submitted, awaiting review
    Late = 1,            // submitted after deadline (if allowed)
    UnderReview = 2,      // teacher opened it / marking in progress
    Graded = 3,           // teacher gave marks + feedback
    ReturnedForRevision = 4 // teacher asked student to resubmit (business rule extension)
}
