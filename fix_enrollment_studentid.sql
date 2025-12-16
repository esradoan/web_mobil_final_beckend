-- Fix Enrollment.StudentId to use Student.Id instead of UserId
-- This script updates existing enrollments to use the correct Student.Id

UPDATE Enrollments e
INNER JOIN Students s ON e.StudentId = s.UserId
SET e.StudentId = s.Id
WHERE e.StudentId != s.Id;

-- Verify the fix
SELECT 
    e.Id AS EnrollmentId,
    e.StudentId AS EnrollmentStudentId,
    s.Id AS StudentId,
    s.UserId AS StudentUserId,
    CASE 
        WHEN e.StudentId = s.Id THEN 'OK'
        ELSE 'NEEDS FIX'
    END AS Status
FROM Enrollments e
INNER JOIN Students s ON e.StudentId = s.UserId OR e.StudentId = s.Id
ORDER BY e.Id;

