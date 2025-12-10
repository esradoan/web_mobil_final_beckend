# Smart Campus - Database Schema

## Entity Relationship Summary
Proje ilişkisel bir veritabanı yapısı kullanır. Temel tablolar ve ilişkileri aşağıdadır:

### Users (Identity)
ASP.NET Core Identity tarafından yönetilen `AspNetUsers` tablosunu temel alır.
- **Id:** PK (int)
- **Email:** Unique
- **PasswordHash:** Hashed Password
- **Role:** Enum (Student, Faculty, Admin)

### UserActivityLogs (Bonus)
Kullanıcı hareketlerini tutar.
- **Id:** PK
- **UserId:** FK (Users)
- **Action:** String (Login, Logout, etc.)
- **Timestamp:** DateTime

### Students
Öğrenci bilgilerini tutar.
- **Id:** PK
- **UserId:** FK (Users) - 1:1 İlişki
- **DepartmentId:** FK (Departments)
- **StudentNumber:** String
- **GPA:** Double

### Faculties (Akademisyenler)
Akademisyen bilgilerini tutar.
- **Id:** PK
- **UserId:** FK (Users) - 1:1 İlişki
- **DepartmentId:** FK (Departments)
- **EmployeeNumber:** String
- **Title:** String

### Departments
Bölüm bilgilerini tutar.
- **Id:** PK
- **Name:** String
- **Code:** String
- **FacultyName:** String (Fakülte Adı, örn: Mühendislik)

### RefreshTokens
JWT Refresh token'larını tutar.
- **Id:** PK
- **Token:** String
- **UserId:** FK (Users)
- **ExpiryDate:** DateTime
