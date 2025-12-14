# Part 2 Frontend Gereksinimleri

Backend tamamlandı! Bu dokümanda frontend geliştiricinin yapması gerekenler özetleniyor.

## 🌐 Backend Bağlantısı

```
Base URL: http://localhost:5000/api/v1
SignalR: ws://localhost:5000/hubs/attendance
```

---

## 📚 MODÜL 1: Akademik Yönetim

### 1.1 Ders Yönetimi (Admin/Faculty)

| Sayfa | Endpoint | Açıklama |
|-------|----------|----------|
| Ders Listesi | `GET /courses` | Tüm dersler |
| Ders Detay | `GET /courses/{id}` | Tekil ders |
| Ders Oluştur | `POST /courses` | Admin only |
| Section Listesi | `GET /courses/{id}/sections` | Ders bölümleri |
| Section Oluştur | `POST /courses/{id}/sections` | Faculty/Admin |

### 1.2 Ders Kaydı (Student)

| Sayfa | Endpoint | Açıklama |
|-------|----------|----------|
| Kayıtlı Derslerim | `GET /enrollments/my-courses` | Aktif dersler |
| Ders Ara | `GET /courses?search=...` | Ders arama |
| Derse Kayıt Ol | `POST /enrollments` | Body: `{sectionId}` |
| Dersten Çekil | `DELETE /enrollments/{id}` | 4 hafta kuralı |

### 1.3 Notlar (Student/Faculty)

| Sayfa | Endpoint | Açıklama |
|-------|----------|----------|
| Notlarım | `GET /grades` | Öğrenci notları |
| Not Gir | `POST /grades` | Faculty, Body: `{enrollmentId, midterm, final, homework}` |
| Transkript | `GET /grades/transcript` | JSON transkript |
| Transkript PDF | `GET /grades/transcript/pdf` | PDF indir |

---

## 📍 MODÜL 2: GPS Tabanlı Yoklama

### 2.1 Yoklama Oturumu (Faculty)

| Sayfa | Endpoint | Açıklama |
|-------|----------|----------|
| Oturumlarım | `GET /attendance/sessions/my-sessions` | Instructor oturumları |
| Oturum Aç | `POST /attendance/sessions` | Body: `{sectionId, date, startTime, endTime}` |
| Oturum Kapat | `PUT /attendance/sessions/{id}/close` | Yoklamayı bitir |
| Yoklama Raporu | `GET /attendance/report/{sectionId}` | Öğrenci listesi |

### 2.2 Yoklama Verme (Student)

| Sayfa | Endpoint | Açıklama |
|-------|----------|----------|
| Yoklama Durumum | `GET /attendance/my-attendance` | Ders bazlı özet |
| GPS ile Yoklama | `POST /attendance/sessions/{id}/checkin` | Body aşağıda |
| QR ile Yoklama | `POST /attendance/sessions/{id}/checkin-qr` | QR + GPS |

**GPS Check-in Body:**
```json
{
  "latitude": 41.0082,
  "longitude": 28.9784,
  "accuracy": 10.5,
  "isMockLocation": false,
  "speed": 0.5
}
```

### 2.3 Mazeret Sistemi

| Sayfa | Endpoint | Açıklama |
|-------|----------|----------|
| Mazeret Gönder | `POST /attendance/excuse-requests` | Form-data (file) |
| Mazeret Listesi | `GET /attendance/excuse-requests` | Faculty |
| Mazeret Onayla | `PUT /attendance/excuse-requests/{id}/approve` | Faculty |
| Mazeret Reddet | `PUT /attendance/excuse-requests/{id}/reject` | Faculty |

---

## 🎁 BONUS ÖZELLİKLER

### Bonus 1: QR Kod (+5 puan)

**Faculty Akışı:**
1. `GET /attendance/sessions/{id}/qr` → QR görsel (Base64)
2. Her 5 saniyede: `POST /attendance/sessions/{id}/qr/refresh` → Yeni QR

**Student Akışı:**
1. Kamera ile QR tara
2. `POST /attendance/sessions/{id}/checkin-qr` gönder

```json
{
  "qrCode": "ABC12345",
  "latitude": 41.0082,
  "longitude": 28.9784,
  "accuracy": 10.5
}
```

### Bonus 2: Real-time Dashboard (+5 puan)

**SignalR Bağlantısı:**
```javascript
import * as signalR from '@microsoft/signalr';

const connection = new signalR.HubConnectionBuilder()
  .withUrl('/hubs/attendance', {
    accessTokenFactory: () => localStorage.getItem('token')
  })
  .withAutomaticReconnect()
  .build();

await connection.start();

// Oturuma katıl
await connection.invoke('JoinSession', sessionId);

// Dinleyiciler
connection.on('StudentCheckedIn', (data) => {
  // { studentId, studentName, checkInTime, distance, isFlagged }
  addToList(data);
});

connection.on('AttendanceCountUpdated', (data) => {
  // { attendedCount, totalStudents, percentage }
  updateCounter(data);
});
```

### Bonus 3: Analytics (+2 puan)

| Sayfa | Endpoint | Rol |
|-------|----------|-----|
| Trend Analizi | `GET /analytics/sections/{id}/trends` | Faculty |
| Risk Analizim | `GET /analytics/my-risk` | Student |
| Öğrenci Riski | `GET /analytics/students/{id}/risk` | Faculty |
| Section Analizi | `GET /analytics/sections/{id}` | Faculty |
| Kampüs Dashboard | `GET /analytics/campus` | Admin |

---

## 🔐 Authentication

Tüm istekler JWT token gerektirir:

```
Authorization: Bearer <token>
```

**Roller:** `Student`, `Faculty`, `Admin`

---

## 📱 Önerilen Sayfalar

### Student
1. Dashboard (Derslerim özet)
2. Derslerim listesi
3. Ders arama + kayıt
4. Notlarım
5. Transkript
6. Yoklama durumum
7. Yoklama ver (GPS/QR)
8. Mazeret gönder
9. Risk analizim

### Faculty
1. Dashboard
2. Derslerim (sections)
3. Yoklama aç
4. Canlı yoklama dashboard (WebSocket)
5. QR görüntüle
6. Yoklama raporu
7. Not girişi
8. Mazeret yönetimi
9. Analytics

### Admin
1. Kampüs analytics
2. Ders yönetimi
3. Section yönetimi
4. Kullanıcı yönetimi

---

## 📦 Önerilen Kütüphaneler

- **HTTP:** Axios veya fetch
- **SignalR:** @microsoft/signalr
- **QR Tarama:** html5-qrcode veya @zxing/browser
- **GPS:** Geolocation API (navigator.geolocation)
- **Harita:** Leaflet veya Google Maps
- **Grafikler:** Chart.js veya Recharts (analytics için)

---

## ⚠️ Önemli Notlar

1. **GPS İzni:** Yoklama için konum izni alınmalı
2. **Kamera İzni:** QR tarama için kamera izni
3. **Token Yenileme:** JWT süresi dolunca refresh
4. **Hata Mesajları:** Tüm error response'lar `{ message, error }` formatında
5. **CORS:** Frontend localhost:5173-5175 portlarında çalışmalı
