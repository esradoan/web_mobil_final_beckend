# Smart Campus - Part 1 Project Overview

## Teknoloji Stack
- **Backend:** .NET 9 (ASP.NET Core Web API)
- **Veritabanı:** MySQL (Entity Framework Core, Code-First)
- **Kimlik Doğrulama:** ASP.NET Core Identity & JWT (Access/Refresh Tokens)
- **Şifreleme:** Identity (PBKDF2) - Güvenli Hashleme
- **Validasyon:** FluentValidation
- **Mapper:** AutoMapper

## Proje Yapısı (Clean Architecture)
```
SmartCampus.sln
│
├── SmartCampus.API/           # Sunum Katmanı (Controllers)
├── SmartCampus.Business/      # İş Katmanı (Services, Validators, Mappings)
├── SmartCampus.DataAccess/    # Veri Erişim Katmanı (DbContext, Repositories)
├── SmartCampus.Entities/      # Varlık Katmanı (Entities, DTOs)
└── SmartCampus.Tests/         # Test Katmanı (xUnit Tests)
```

## Temel Özellikler
1. **Kimlik Yönetimi:**
   - Kayıt Ol, Giriş Yap, Çıkış Yap.
   - Token Yenileme (Refresh Token).
   - Email Doğrulama ve Şifre Sıfırlama (HTML Şablonlu).
   - Account Lockout (5 hatalı giriş sonrası 15 dk kilit).
   - Bonus: Password Strength Meter.
   
2. **Kullanıcı Yönetimi:**
   - Profil Görüntüleme/Güncelleme.
   - Profil Resmi Yükleme (`wwwroot/uploads`).
   - Admin: Tüm kullanıcıları listeleme.

3. **Loglama:**
   - User Activity Log (Veritabanı tabanlı loglama).

## Kurulum ve Çalıştırma
1. `docker-compose up --build` ile veritabanı ve API'yi kaldırın.
2. `http://localhost:5000/swagger` adresinden API dokümantasyonuna erişin.
