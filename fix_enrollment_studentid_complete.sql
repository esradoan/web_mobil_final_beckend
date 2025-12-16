-- Fix Enrollment.StudentId to use Student.Id instead of UserId
-- Step 1: Drop the incorrect foreign key constraint
ALTER TABLE Enrollments DROP FOREIGN KEY IF EXISTS FK_Enrollments_AspNetUsers_StudentId;

-- Step 2: Update existing enrollments to use the correct Student.Id
UPDATE Enrollments e
INNER JOIN Students s ON e.StudentId = s.UserId
SET e.StudentId = s.Id
WHERE e.StudentId != s.Id;

-- Step 3: Add the correct foreign key constraint (if it doesn't exist)
-- First, check if the constraint already exists with a different name
-- If FK_Enrollments_Students_StudentId doesn't exist, create it
ALTER TABLE Enrollments 
ADD CONSTRAINT FK_Enrollments_Students_StudentId 
FOREIGN KEY (StudentId) REFERENCES Students(Id) ON DELETE CASCADE;

-- Step 4: Verify the fix
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
INNER JOIN Students s ON e.StudentId = s.Id OR e.StudentId = s.UserId
ORDER BY e.Id;

