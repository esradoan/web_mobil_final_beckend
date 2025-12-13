# Akıllı Kampüs Ekosistem Yönetim Platformu - Proje Gereksinimleri

## 📋 Proje Genel Bilgileri
- **Ders**: Web ve Mobil Programlama
- **Öğretim Üyesi**: Dr. Öğretim Üyesi Mehmet Sevri
- **Dönem**: Güz 2024-2025
- **Final Teslim**: 28 Aralık 2025, 23:59
- **Sunum**: 29 Aralık 2025

## ✅ Part 1: Tamamlandı
- ✅ Authentication & User Management
  - Kullanıcı kaydı (öğrenci, öğretim üyesi, admin)
  - Email doğrulama sistemi
  - JWT tabanlı login/logout
  - Refresh token mekanizması
  - Şifre sıfırlama (forgot password)
  - Profil yönetimi (CRUD)
  - Profil fotoğrafı yükleme
  - Admin sayfası

## 🎯 Part 2: Akademik Yönetim ve GPS Yoklama (ŞİMDİ)

**Teslim Tarihi:** 15 Aralık 2025 (Pazar), 23:59  
**Süre:** 7 gün  
**Ağırlık:** %25

### 2.1 Academic Management (Zorunlu - P0)
**Özellikler:**
- [ ] Ders kataloğu (course catalog) görüntüleme
- [ ] Ders detayları (açıklama, kredi, ECTS, önkoşullar)
- [ ] Ders bölümü (section) yönetimi
- [ ] Derse kayıt olma (enrollment)
- [ ] Önkoşul kontrolü (recursive prerequisite checking)
- [ ] Çakışma kontrolü (schedule conflict detection)
- [ ] Kapasite kontrolü (atomic increment)
- [ ] Dersi bırakma (drop course)
- [ ] Kayıtlı derslerim listesi
- [ ] Not görüntüleme (öğrenci)
- [ ] Not girişi (öğretim üyesi)
- [ ] Transkript, öğrenci belgesi görüntüleme ve PDF indirme
- [ ] Akademik takvim, kişisel ders takvimi, duyuru görüntüleme

**Teknik Detaylar:**
- Önkoşul kontrolü: Graph traversal algoritması (BFS/DFS)
- Schedule conflict: Time overlap detection algorithm
- Capacity: Database transaction ve row-level locking
- PDF generation: PDFKit veya Puppeteer (C# için: iTextSharp veya QuestPDF)
- Grade calculation: Otomatik harf notu hesaplama (4.0 scale)

### 2.2 GPS-Based Attendance System (Zorunlu - P0)
**Özellikler:**
- [ ] Yoklama oturumu açma (öğretim üyesi)
- [ ] Derslik GPS koordinatları otomatik alınır
- [ ] Geofencing radius (varsayılan 15m)
- [ ] QR kod alternatifi (5 saniyede bir yenilenecek)
- [ ] QR okunduğunda konum doğrulama ile birlikte "var" yazacak (backup)
- [ ] Öğrenci yoklama verme
- [ ] Tarayıcı GPS API ile konum alma
- [ ] Sunucuda mesafe hesaplama (Haversine formula)
- [ ] GPS spoofing tespiti (mock location detection)
- [ ] Yoklama durumu görüntüleme (öğrenci)
- [ ] Yoklama raporları (öğretim üyesi)
- [ ] Devamsızlık uyarıları (otomatik email/SMS)
- [ ] Mazeret bildirme ve onaylama

**Teknik Detaylar:**
- GPS API: Navigator.geolocation.getCurrentPosition()
  - Doğruluk: high accuracy mode, timeout: 10s
- Mesafe hesaplama: Haversine formula
  ```
  distance = 2 * R * asin(sqrt(sin²(Δlat/2) + cos(lat1) * cos(lat2) * sin²(Δlon/2)))
  ```
- Spoofing detection:
  - IP address validation (kampüs IP aralığı)
  - Öğrenci yoklamaya sadece kampüs ağına bağlı iken bağlanabilmelidir
  - Gelen IP belirleme
  - Mock location flag kontrolü
  - Velocity check (önceki konumdan impossible travel)
  - Device sensor tutarlılığı
- Fraud flagging: Şüpheli aktiviteler otomatik işaretlenir

## 📚 Teknik Gereksinimler

### Frontend (React)
- ✅ React 18+ (Hooks kullanımı zorunlu)
- ✅ React Router v6 (client-side routing)
- ✅ State Management: Context API + useReducer
- ✅ HTTP Client: Axios
- ✅ Styling: Tailwind CSS
- ✅ Form Handling: React Hook Form + Zod validation
- [ ] Charts: Chart.js, Recharts VEYA Victory
- [ ] QR Code: qrcode.react
- [ ] Maps: Leaflet VEYA Google Maps API

### Backend (.NET Core) - ⚠️ MEVCUT YAPI KORUNACAK - DEĞİŞTİRİLMEYECEK
**ÖNEMLİ:** Proje gereksinimlerinde Node.js + PostgreSQL yazıyor ama biz backend'i .NET Core + MySQL ile yaptık. Mevcut backend yapısı mükemmel ve korunacak. PostgreSQL'e veya Node.js'e geçiş yapılmayacak.

- ✅ .NET 8.0
- ✅ Entity Framework Core (Pomelo.EntityFrameworkCore.MySql)
- ✅ MySQL (PostgreSQL değil - mevcut yapı korunacak)
- ✅ JWT Authentication (jsonwebtoken yerine Microsoft.AspNetCore.Authentication.JwtBearer)
- ✅ Password Hashing: .NET Identity PasswordHasher (bcrypt yerine)
- ✅ File Upload: IFormFile (Multer yerine)
- ✅ Email: SMTP (.NET Mail - NodeMailer yerine)
- ✅ Validation: FluentValidation (Joi/express-validator yerine)
- ✅ API Documentation: Swagger/OpenAPI
- ✅ AutoMapper (manuel mapping yerine)
- ✅ Repository Pattern (GenericRepository, UnitOfWork)
- ✅ Exception Middleware
- ✅ CORS Configuration

### Veritabanı Gereksinimleri
**Minimum Tablolar (30+):**
- ✅ users, students, faculty, admins, departments
- [ ] courses, course_sections, enrollments
- [ ] attendance_sessions, attendance_records, excuse_requests
- [ ] classrooms, reservations, schedules
- [ ] meal_menus, meal_reservations, wallets, transactions
- [ ] events, event_registrations
- [ ] notifications, notification_preferences
- [ ] iot_sensors, sensor_data
- [ ] audit_logs, password_resets, email_verifications, session_tokens

**Veritabanı Tasarım Kuralları:**
- Normalization: 3NF minimum
- Foreign keys: CASCADE ve RESTRICT uygun kullanımı
- Indexes: Performance için gerekli alanlara index
- Constraints: CHECK, UNIQUE, NOT NULL constraints
- JSON: MySQL JSON column type (PostgreSQL JSONB yerine) - Flexible data için kullanılabilir (schedule, metadata)
- Soft delete: IsDeleted boolean veya DeletedAt timestamp pattern (bazı tablolarda)

### API Gereksinimleri
- ✅ RESTful API Standards
- ✅ Base URL: /api/v1/
- ✅ HTTP Methods: GET, POST, PUT, PATCH, DELETE
- ✅ Status Codes: 200, 201, 204, 400, 401, 403, 404, 409, 500
- ✅ Response Format: JSON (consistent structure)
- ✅ Error Handling: Standardized error responses
- [ ] Pagination: page, limit, sort parameters
- [ ] Filtering: Query parameters
- [ ] Rate Limiting: ASP.NET Core Rate Limiting middleware (bonus)

**Minimum 60+ Endpoints:** Tüm modüller için CRUD operations + özel endpoint'ler

### Güvenlik Gereksinimleri
- ✅ JWT token-based auth
- ✅ Refresh token mechanism
- ✅ Token expiration handling
- ✅ Secure password storage
- ✅ Role-based access control (RBAC)
- ✅ Middleware authentication guards
- ✅ Route protection (frontend & backend)
- ✅ Backend validation (FluentValidation)
- ✅ Frontend validation (React Hook Form + Zod)
- ✅ SQL injection prevention (EF Core parameterized queries)
- ✅ XSS prevention (input sanitization)
- ✅ CORS configuration
- ✅ Environment Variables (.env)

### Testing Gereksinimleri
- [ ] Unit Tests: Critical business logic (minimum 50+ tests)
- [ ] Integration Tests: API endpoints (minimum 30+ tests)
- [ ] E2E Tests: Critical user flows (minimum 5 scenarios - bonus)
- [ ] Backend: Minimum %85 code coverage
- [ ] Frontend: Minimum %75 code coverage

### Performance Gereksinimleri
- Page load time: < 3 saniye (initial load)
- API response time: < 500ms (average)
- Database query time: < 100ms (optimized queries)
- Concurrent users: 100+ kullanıcı desteği

**Optimization Teknikler:**
- Database indexing
- Query optimization
- Lazy loading (React components)
- Code splitting (React.lazy)
- Image optimization
- Caching (Redis - bonus)

## 📝 ÖNEMLİ NOTLAR

### Backend Yapısı - DEĞİŞTİRİLMEYECEK
- ✅ **Backend: .NET Core 8.0** (Proje gereksinimlerinde Node.js yazıyor ama biz .NET kullanıyoruz)
- ✅ **Veritabanı: MySQL** (Proje gereksinimlerinde PostgreSQL yazıyor ama biz MySQL kullanıyoruz)
- ✅ Mevcut backend yapısı mükemmel - değiştirilmeyecek
- ✅ Repository Pattern, UnitOfWork, AutoMapper mevcut
- ✅ FluentValidation, Exception Middleware mevcut
- ✅ JWT Authentication, .NET Identity mevcut

### Frontend Yapısı
- ✅ React 18+ (Proje gereksinimlerine uygun)
- ✅ React Router v6
- ✅ Context API + useReducer
- ✅ Axios
- ✅ Tailwind CSS
- ✅ React Hook Form + Zod

### Part 2 Durumu
- Part 2'ye geçiş yapıldı - Academic Management ve GPS Attendance System geliştirilecek
- Backend .NET Core + MySQL ile devam edilecek
- Frontend React ile devam edilecek

