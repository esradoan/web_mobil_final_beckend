# Analytics & Reporting Guide

## 📊 Available Reports

### 1. Section Attendance Report
**Audience**: Faculty, Admin
**Description**: A detailed breakdown of student attendance provided for a specific course section.

**Data Points**:
- Student Name & ID
- Total Sessions Held
- Sessions Attended
- Attendance Rate (%)
- Risk Status (Low/High)

**Export Formats**:
- **PDF**: Formatted for printing and official records.
- **Excel (CSV)**: Raw data for custom analysis using spreadsheet software.

### 2. Campus Overview (Admin Dashboard)
**Audience**: Admin
**Description**: High-level metrics for the entire university.

**Data Points**:
- Active Students Count
- Total Courses Offered
- Daily Active Sessions
- Average Campus Attendance Rate

## 📥 How to Export

### Via API (Swagger/Postman)
1. **Login** to get a Bearer Token.
2. **Find Section ID** via `GET /sections`.
3. **Call Export Endpoint**:
   - `GET /api/v1/analytics/sections/{id}/export/pdf`
   - `GET /api/v1/analytics/sections/{id}/export/excel`
4. **Download**: The response will trigger a file download (`AttendanceReport_X.pdf`).

## 📈 Interpreting Data

- **Attendance Rate**: `< 70%` is typically flagged as "At Risk" (Yellow/Red).
- **Session Count**: Ensure this matches the academic calendar. Discrepancies might indicate missed attendance taking by the instructor.
