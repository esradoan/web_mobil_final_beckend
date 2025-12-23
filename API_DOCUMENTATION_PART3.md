# API Documentation - Part 3

Akıllı Kampüs Sistemi Part 3 API Dokümantasyonu

**Base URL:** `http://localhost:5226/api/v1`

**Authentication:** Tüm endpoint'ler (aksi belirtilmedikçe) JWT Bearer token gerektirir.

---

## 📋 İçindekiler

1. [Yemek Servisi (Meals)](#-yemek-servisi-meals)
2. [Etkinlik Yönetimi (Events)](#-etkinlik-yönetimi-events)
3. [Ders Programı (Scheduling)](#-ders-programı-scheduling)
4. [Cüzdan/Ödeme (Wallet)](#-cüzdanödeme-wallet)

---

## 🍽️ Yemek Servisi (Meals)

Base path: `/api/v1/meals`

### Yemekhaneler

#### GET /meals/cafeterias
Aktif yemekhaneleri listele.

**Authorization:** Gerekli değil (AllowAnonymous)

**Response:**
```json
{
  "data": [
    {
      "id": 1,
      "name": "Merkez Yemekhane",
      "location": "Ana Kampüs",
      "capacity": 500,
      "openingHours": "07:00-22:00"
    }
  ]
}
```

---

### Menüler

#### GET /meals/menus
Menü listesi (tarih ve yemekhane filtresi).

**Authorization:** Gerekli değil (AllowAnonymous)

**Query Parameters:**
| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| date | DateTime | Hayır | Menü tarihi |
| cafeteriaId | int | Hayır | Yemekhane ID |

**Response:**
```json
{
  "data": [
    {
      "id": 1,
      "date": "2024-01-15",
      "mealType": "Lunch",
      "items": "Mercimek Çorbası, Tavuk Sote, Pilav, Salata",
      "price": 25.00,
      "cafeteriaId": 1
    }
  ]
}
```

---

#### GET /meals/menus/{id}
Menü detayı.

**Authorization:** Gerekli değil (AllowAnonymous)

**Path Parameters:**
| Parametre | Tip | Açıklama |
|-----------|-----|----------|
| id | int | Menü ID |

**Response:** Tek menü objesi

**Errors:**
- `404 Not Found` - Menü bulunamadı

---

#### POST /meals/menus
Yeni menü oluştur.

**Authorization:** Admin

**Request Body:**
```json
{
  "date": "2024-01-20",
  "mealType": "Lunch",
  "items": "Ezogelin Çorbası, Karnıyarık, Bulgur Pilavı",
  "price": 30.00,
  "cafeteriaId": 1,
  "maxReservations": 200
}
```

**Response:** `201 Created` - Oluşturulan menü

---

#### PUT /meals/menus/{id}
Menü güncelle.

**Authorization:** Admin

**Path Parameters:**
| Parametre | Tip | Açıklama |
|-----------|-----|----------|
| id | int | Menü ID |

**Request Body:** Güncellenecek alanlar

**Response:** Güncellenmiş menü

---

#### DELETE /meals/menus/{id}
Menü sil.

**Authorization:** Admin

**Response:**
```json
{
  "message": "Menu deleted successfully"
}
```

---

### Rezervasyonlar

#### POST /meals/reservations
Yemek rezervasyonu yap.

**Authorization:** Gerekli (Login)

**Business Rules:**
- Burslu öğrenci: Günde maksimum 2 öğün
- Ücretli: Cüzdan bakiye kontrolü yapılır

**Request Body:**
```json
{
  "menuId": 5,
  "paymentMethod": "wallet"
}
```

**Response:** `201 Created` - Rezervasyon bilgileri + QR kod

**Errors:**
- `400 Bad Request` - Yetersiz bakiye / Günlük limit aşıldı

---

#### DELETE /meals/reservations/{id}
Rezervasyon iptali.

**Authorization:** Gerekli (Login)

**Business Rules:**
- En az 2 saat önce iptal edilmeli
- Ücretli ise otomatik iade yapılır

**Response:**
```json
{
  "message": "Reservation cancelled successfully"
}
```

---

#### GET /meals/reservations/my-reservations
Kullanıcının rezervasyonları.

**Authorization:** Gerekli (Login)

**Response:**
```json
{
  "data": [
    {
      "id": 1,
      "menuId": 5,
      "date": "2024-01-15",
      "mealType": "Lunch",
      "status": "Reserved",
      "qrCode": "RES-ABC123",
      "paymentStatus": "Paid"
    }
  ]
}
```

---

#### POST /meals/reservations/validate
QR kod ile rezervasyon doğrulama (Status değiştirmez).

**Authorization:** Admin, Faculty

**Request Body:**
```json
{
  "qrCode": "RES-ABC123"
}
```

**Response:**
```json
{
  "message": "Reservation validated",
  "reservation": { ... }
}
```

---

#### POST /meals/reservations/use
QR kod ile yemek kullanımı (Status'u "Used" yapar).

**Authorization:** Admin, Faculty

**Request Body:**
```json
{
  "qrCode": "RES-ABC123"
}
```

**Response:**
```json
{
  "message": "Meal confirmed",
  "reservation": { ... }
}
```

---

## 🎉 Etkinlik Yönetimi (Events)

Base path: `/api/v1/events`

### Etkinlikler

#### GET /events
Etkinlik listesi.

**Authorization:** Gerekli değil (AllowAnonymous)

**Query Parameters:**
| Parametre | Tip | Zorunlu | Açıklama |
|-----------|-----|---------|----------|
| category | string | Hayır | Etkinlik kategorisi (seminer, konser, workshop) |
| date | DateTime | Hayır | Etkinlik tarihi |

**Response:**
```json
{
  "data": [
    {
      "id": 1,
      "title": "Yapay Zeka Semineri",
      "description": "AI ve ML üzerine seminer",
      "category": "seminer",
      "date": "2024-02-01T14:00:00",
      "location": "Konferans Salonu A",
      "capacity": 100,
      "registeredCount": 45
    }
  ]
}
```

---

#### GET /events/{id}
Etkinlik detayı.

**Authorization:** Gerekli değil (AllowAnonymous)

**Response:** Tek etkinlik objesi

---

#### POST /events
Yeni etkinlik oluştur.

**Authorization:** Admin, Faculty

**Request Body:**
```json
{
  "title": "Kariyer Günleri",
  "description": "Teknoloji şirketleri ile kariyer fırsatları",
  "category": "kariyer",
  "date": "2024-03-15T10:00:00",
  "endDate": "2024-03-15T17:00:00",
  "location": "Ana Salon",
  "capacity": 200,
  "requiresRegistration": true
}
```

**Response:** `201 Created`

---

#### PUT /events/{id}
Etkinlik güncelle.

**Authorization:** Admin, Faculty

---

#### DELETE /events/{id}
Etkinlik iptal et.

**Authorization:** Admin, Faculty

**Response:**
```json
{
  "message": "Event cancelled successfully"
}
```

---

### Kayıtlar

#### POST /events/{id}/register
Etkinliğe kayıt ol.

**Authorization:** Gerekli (Login)

**Response:** Kayıt bilgileri + QR kod

**Errors:**
- `400 Bad Request` - Kapasite dolu / Zaten kayıtlı

---

#### DELETE /events/registrations/{registrationId}
Kayıt iptal et.

**Authorization:** Gerekli (Login)

---

#### GET /events/{id}/registrations
Etkinliğe kayıtlı kullanıcıları listele.

**Authorization:** Admin, Faculty

---

#### GET /events/my-events
Kullanıcının kayıtlı olduğu etkinlikler.

**Authorization:** Gerekli (Login)

---

#### POST /events/{eventId}/checkin
QR kod ile check-in.

**Authorization:** Admin, Faculty

**Request Body:**
```json
{
  "qrCode": "EVT-XYZ789"
}
```

**Response:**
```json
{
  "message": "Check-in successful",
  "registration": { ... }
}
```

---

## 📅 Ders Programı (Scheduling)

Base path: `/api/v1/scheduling`

### Program Oluşturma

#### POST /scheduling/generate
CSP algoritması ile otomatik ders programı oluştur.

**Authorization:** Admin

**Request Body:**
```json
{
  "semester": "fall",
  "year": 2024,
  "sectionIds": [1, 2, 3, 4, 5]
}
```

**Response:**
```json
{
  "success": true,
  "message": "Schedule generated successfully",
  "scheduledCount": 5,
  "failedCount": 0,
  "schedules": [...],
  "conflicts": []
}
```

---

#### POST /scheduling/generate/genetic
Genetik Algoritma ile ders programı oluştur (daha iyi optimizasyon).

**Authorization:** Admin

**Request Body:**
```json
{
  "semester": "fall",
  "year": 2024,
  "sectionIds": [1, 2, 3],
  "populationSize": 50,
  "generations": 100
}
```

---

### Program Görüntüleme

#### GET /scheduling
Dönem programını görüntüle.

**Authorization:** Gerekli (Login)

**Query Parameters:**
| Parametre | Tip | Varsayılan | Açıklama |
|-----------|-----|------------|----------|
| semester | string | "fall" | Dönem (fall/spring/summer) |
| year | int | Mevcut yıl | Yıl |

**Response:**
```json
{
  "data": [
    {
      "id": 1,
      "sectionId": 10,
      "courseCode": "CENG101",
      "courseName": "Programlamaya Giriş",
      "instructorName": "Dr. Ahmet Yılmaz",
      "dayOfWeek": 1,
      "dayName": "Pazartesi",
      "startTime": "09:00:00",
      "endTime": "11:00:00",
      "classroomName": "B101",
      "building": "Mühendislik"
    }
  ]
}
```

---

#### GET /scheduling/{scheduleId}
Tek bir schedule kaydını görüntüle.

**Authorization:** Gerekli (Login)

---

#### GET /scheduling/my-schedule
Kullanıcının kendi programı (öğrenci veya öğretim üyesi).

**Authorization:** Gerekli (Login)

**Query Parameters:**
| Parametre | Tip | Varsayılan |
|-----------|-----|------------|
| semester | string | "fall" |
| year | int | Mevcut yıl |

---

#### GET /scheduling/my-schedule/ical
iCal formatında dışa aktar (.ics dosyası).

**Authorization:** Gerekli (Login)

**Response:** `text/calendar` dosyası

---

## 💳 Cüzdan/Ödeme (Wallet)

Base path: `/api/v1/wallet`

### Bakiye İşlemleri

#### GET /wallet/balance
Bakiye sorgula.

**Authorization:** Gerekli (Login)

**Response:**
```json
{
  "userId": 1,
  "balance": 150.50,
  "currency": "TRY"
}
```

---

#### POST /wallet/topup
Para yükleme oturumu oluştur.

**Authorization:** Gerekli (Login)

**Business Rules:**
- Minimum: 50 TRY
- Maksimum: 5000 TRY

**Request Body:**
```json
{
  "amount": 100.00
}
```

**Response:**
```json
{
  "success": true,
  "paymentUrl": "https://payment.example.com/pay/REF123",
  "paymentReference": "REF123",
  "amount": 100.00
}
```

---

#### GET /wallet/topup/complete
Ödeme tamamlama (Demo endpoint).

**Authorization:** Gerekli değil (AllowAnonymous)

**Query Parameters:**
| Parametre | Tip | Açıklama |
|-----------|-----|----------|
| ref | string | Ödeme referansı |

**Response:**
```json
{
  "message": "Payment completed successfully"
}
```

---

#### POST /wallet/topup/webhook
Ödeme webhook'u (Stripe/PayTR entegrasyonu).

**Authorization:** Gerekli değil (AllowAnonymous)

**Request Body:**
```json
{
  "paymentReference": "REF123",
  "success": true
}
```

**Response:**
```json
{
  "received": true
}
```

> ⚠️ **Not:** Production ortamında webhook imzası doğrulanmalıdır.

---

#### GET /wallet/transactions
İşlem geçmişi.

**Authorization:** Gerekli (Login)

**Query Parameters:**
| Parametre | Tip | Varsayılan |
|-----------|-----|------------|
| page | int | 1 |
| pageSize | int | 20 |

**Response:**
```json
{
  "data": [
    {
      "id": 1,
      "type": "TopUp",
      "amount": 100.00,
      "description": "Bakiye yükleme",
      "createdAt": "2024-01-15T10:30:00"
    },
    {
      "id": 2,
      "type": "Payment",
      "amount": -25.00,
      "description": "Yemek ödemesi",
      "createdAt": "2024-01-15T12:00:00"
    }
  ],
  "page": 1,
  "pageSize": 20
}
```

---

#### POST /wallet/add-balance
Manuel bakiye ekleme.

**Authorization:** Admin

**Request Body:**
```json
{
  "userId": 5,
  "amount": 50.00,
  "description": "Burs yüklemesi"
}
```

---

## 🔐 Hata Kodları

| HTTP Kodu | Error | Açıklama |
|-----------|-------|----------|
| 400 | BadRequest | Geçersiz istek |
| 401 | Unauthorized | Token eksik veya geçersiz |
| 403 | Forbidden | Yetki yetersiz |
| 404 | NotFound | Kaynak bulunamadı |
| 500 | InternalError | Sunucu hatası |

---

## 📝 Örnek cURL İstekleri

### Yemek Rezervasyonu Yap
```bash
curl -X POST http://localhost:5226/api/v1/meals/reservations \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{"menuId": 1, "paymentMethod": "wallet"}'
```

### Etkinliğe Kayıt Ol
```bash
curl -X POST http://localhost:5226/api/v1/events/1/register \
  -H "Authorization: Bearer <TOKEN>"
```

### Ders Programı Oluştur
```bash
curl -X POST http://localhost:5226/api/v1/scheduling/generate \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{"semester": "fall", "year": 2024, "sectionIds": [1,2,3]}'
```

### Para Yükle
```bash
curl -X POST http://localhost:5226/api/v1/wallet/topup \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{"amount": 100}'
```
