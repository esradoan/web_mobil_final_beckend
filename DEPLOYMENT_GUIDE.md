# Deployment Guide - Railway & Local Development

Bu dokümantasyon, projenin hem **local development** hem de **Railway production** ortamında çalışması için gerekli yapılandırmaları açıklar.

## 🔧 Backend Yapılandırması

### Local Development

**appsettings.Development.json** dosyası otomatik olarak kullanılır:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=smart_campus_db;User=root;Password=1234;Port=3306;"
  }
}
```

**Gereksinimler:**
- MySQL Server çalışıyor olmalı
- `smart_campus_db` database'i oluşturulmuş olmalı
- Root kullanıcısı şifresi: `1234`

### Railway Production

Railway'de **Environment Variables** olarak ayarlayın:

1. Railway Dashboard → Projeniz → **Variables** sekmesi
2. Yeni variable ekleyin:

```
Name:  ConnectionStrings__DefaultConnection
Value: Server=your-mysql-host;Database=campus_db;User=campus_user;Password=campus_password;Port=3307;
```

**Not:** `__` (double underscore) kullanın! ASP.NET Core bunu `ConnectionStrings:DefaultConnection` olarak yorumlar.

**Örnek Railway MySQL Connection String:**
```
Server=containers-us-west-xxx.railway.app;Database=railway;User=root;Password=xxxxx;Port=3306;
```

### JWT Settings (Opsiyonel)

Railway'de JWT secret'ı değiştirmek isterseniz:

```
Name:  JwtSettings__Secret
Value: YourSuperSecretKeyForProduction_MustBeVeryLong_AtLeast32Chars
```

### CORS Settings (Opsiyonel)

Railway'de frontend URL'ini CORS'a eklemek için:

```
Name:  RailwayFrontendUrl
Value: https://your-frontend.railway.app
```

**Not:** Eğer bu variable ayarlanmazsa, sadece local URL'ler kullanılır.

## 🎨 Frontend Yapılandırması

### Local Development

**1. `.env.local` dosyası oluşturun:**

Frontend klasöründe (`web_mobil_final_frontend`) `.env.local` dosyası oluşturun:

```env
VITE_API_BASE_URL=http://localhost:5226/api/v1
```

**2. Frontend'i başlatın:**
```bash
npm run dev
```

### Railway Production

**1. Railway Dashboard → Frontend Projeniz → Variables**

Yeni variable ekleyin:

```
Name:  VITE_API_BASE_URL
Value: https://your-backend.railway.app/api/v1
```

**Örnek:**
```
Name:  VITE_API_BASE_URL
Value: https://smartcampus-backend-production.up.railway.app/api/v1
```

**2. Build ve Deploy:**

Railway otomatik olarak build eder, ancak manuel build için:

```bash
npm run build
```

## 📋 Railway Deployment Checklist

### Backend (Railway)

- [ ] MySQL servisi Railway'de oluşturuldu
- [ ] Environment variable eklendi: `ConnectionStrings__DefaultConnection`
- [ ] JWT Secret ayarlandı (opsiyonel)
- [ ] Port ayarı: Railway otomatik `PORT` environment variable'ı sağlar
- [ ] CORS ayarları: `RailwayFrontendUrl` environment variable'ı eklendi (opsiyonel)

### Frontend (Railway)

- [ ] Environment variable eklendi: `VITE_API_BASE_URL`
- [ ] Backend URL'i doğru ayarlandı
- [ ] Build başarılı
- [ ] Static files serve ediliyor

## 🔍 Troubleshooting

### Backend MySQL Bağlantı Hatası

**Local:**
- MySQL servisinin çalıştığını kontrol edin
- `smart_campus_db` database'inin var olduğunu kontrol edin
- `appsettings.Development.json` dosyasını kontrol edin

**Railway:**
- Environment variable'ın doğru formatta olduğunu kontrol edin (`__` kullanın)
- MySQL servisinin Railway'de çalıştığını kontrol edin
- Connection string'deki host, port, database, user, password bilgilerini kontrol edin

### Frontend Backend Bağlantı Hatası

**Local:**
- `.env.local` dosyasının var olduğunu kontrol edin
- Backend'in `http://localhost:5226` adresinde çalıştığını kontrol edin
- Frontend'i yeniden başlatın (`.env.local` değişiklikleri için gerekli)

**Railway:**
- `VITE_API_BASE_URL` environment variable'ının ayarlandığını kontrol edin
- Backend URL'inin doğru olduğunu kontrol edin (HTTPS kullanın)
- Build loglarını kontrol edin

## 🚀 Hızlı Başlangıç

### Local Development

**Backend:**
```bash
# Visual Studio'da backend'i başlatın
# Swagger: http://localhost:5226/swagger
```

**Frontend:**
```bash
cd web_mobil_final_frontend
# .env.local dosyasını oluşturun (yukarıdaki içerikle)
npm run dev
# Frontend: http://localhost:5173
```

### Railway Production

**Backend:**
1. Railway'de backend servisi oluşturun
2. MySQL servisi ekleyin
3. Environment variable ekleyin: `ConnectionStrings__DefaultConnection`
4. Deploy edin

**Frontend:**
1. Railway'de frontend servisi oluşturun
2. Environment variable ekleyin: `VITE_API_BASE_URL`
3. Deploy edin

## 📝 Notlar

- **Local:** `appsettings.Development.json` otomatik kullanılır
- **Production:** Environment variable'lar `appsettings.json`'u override eder
- **Frontend:** `.env.local` local için, Railway'de environment variable kullanın
- **CORS:** Backend'de frontend URL'i `Program.cs`'de tanımlı olmalı

