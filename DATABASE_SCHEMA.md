# Database Schema Documentation

## 🗄️ Overview
The database uses **MySQL 8.0** and is managed via Entity Framework Core Migrations.

## 📊 Entity Relationship Diagram (ERD) -> Text Representation

- **User** (1) ---- (*) **Enrollment** (*) ---- (1) **CourseSection**
- **Course** (1) ---- (*) **CourseSection**
- **User** (1) ---- (1) **Student/Instructor** Profile
- **IoTSensor** (1) ---- (*) **SensorData**

## 📋 Tables & Schemas

### Identity & Users
- **AspNetUsers**: Base user table (Identity).
- **Users**: Extended user profile (FirstName, LastName).
- **Students**: Student-specific info (StudentNumber, GPA).
- **Instructors**: Faculty-specific info.

### Academic
- **Courses**: Catalog (Code, Name, Credits).
- **CourseSections**: Instances of courses (Semester, Year, Schedule).
- **Enrollments**: Link between Student & Section (Grades, Status).
- **Departments**: Academic departments.

### Attendance
- **AttendanceSessions**: Created by Instructor (StartTime, EndTime, QRCode).
- **AttendanceRecords**: Student check-ins (Time, Location).

### Notifications (Part 4)
- **Notifications**:
  - `Id` (PK)
  - `UserId` (FK)
  - `Title`, `Message`
  - `IsRead` (bool)
  - `Type` (Enrollment, Grade, System)
  
- **NotificationPreferences**:
  - `UserId` (PK, FK)
  - `EmailEnabled`, `PushEnabled`

### IoT System (Part 4)
- **IoTSensors**:
  - `Id` (PK)
  - `Name`, `Location`, `Type`
  - `LastValue`, `LastUpdate`
  
- **SensorData**:
  - `Id` (PK)
  - `SensorId` (FK)
  - `Value`, `Unit`
  - `Timestamp`

## 🔑 Key Indexes
- `IX_Enrollments_StudentId_SectionId` (Unique: Prevent double enrollment)
- `IX_AttendanceRecords_StudentId_SessionId` (Unique: Prevent double check-in)
- `IX_IoTSensors_Type` (Performance: Filtering sensors)
