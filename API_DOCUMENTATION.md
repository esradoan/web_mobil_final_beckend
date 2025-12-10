# Smart Campus - API Documentation

Bu proje **Swagger (OpenAPI)** kullanmaktadır. Projeyi çalıştırdıktan sonra aşağıdaki adresten interaktif dokümantasyona ulaşabilirsiniz:

**Swagger UI:** [http://localhost:5000/swagger](http://localhost:5000/swagger)

## Önemli Endpoint'ler

### Auth
- `POST /api/v1/auth/register`: Yeni kullanıcı kaydı.
- `POST /api/v1/auth/login`: Giriş (Access + Refresh Token döner).
- `POST /api/v1/auth/refresh`: Token yenileme.
- `POST /api/v1/auth/verify-email`: Email doğrulama.
- `POST /api/v1/auth/forgot-password`: Şifre sıfırlama linki gönderir.
- `POST /api/v1/auth/reset-password`: Yeni şifreyi belirler.
- `POST /api/v1/auth/password-strength`: Şifre gücünü ölçer (Bonus).

### Users
- `GET /api/v1/users/me`: Profil bilgilerini getirir.
- `PUT /api/v1/users/me`: Profil bilgilerini günceller.
- `POST /api/v1/users/me/profile-picture`: Profil resmi yükler.
- `GET /api/v1/users`: (Admin) Kullanıcı listesi.

## Yetkilendirme
Çoğu endpoint `Bearer Token` gerektirir. Login olduktan sonra gelen `accessToken` değerini Swagger'daki "Authorize" butonuna `Bearer <token>` formatında girmelisiniz.
