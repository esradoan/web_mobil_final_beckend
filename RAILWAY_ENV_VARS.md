# Railway Environment Variables

Railway'de bu environment variable'ları eklemen gerekiyor:

## Database (MySQL) - OTOMATIK
Railway MySQL servisi eklendiğinde otomatik olarak şu variable'lar set edilir:
- `MYSQLHOST` - MySQL host adresi
- `MYSQLPORT` - MySQL port (genellikle 3306)
- `MYSQLUSER` - MySQL kullanıcı adı
- `MYSQLPASSWORD` - MySQL şifresi
- `MYSQLDATABASE` - Database adı (genellikle "railway")
- `MYSQL_URL` - Alternatif connection string formatı

**Not:** Backend otomatik olarak bu variable'ları kullanır. Elle bir şey yapmana gerek yok!

Eğer manuel connection string kullanmak istersen:
```
ConnectionStrings__DefaultConnection=Server=HOST;Database=DB;User=USER;Password=PASS;Port=PORT;
```

## Frontend URL (CORS için)
```
FRONTEND_URL=https://your-frontend-app.railway.app
```
**Not:** Birden fazla URL varsa virgülle ayır: `FRONTEND_URL=https://app1.railway.app,https://app2.railway.app`

Alternatif (backward compatibility):
```
RailwayFrontendUrl=https://your-frontend-app.railway.app
FrontendUrl=https://your-frontend-app.railway.app
```

## SMTP Settings (Gmail) - Gerçek Email İçin
```
SmtpSettings__Host=smtp.gmail.com
SmtpSettings__Port=587
SmtpSettings__Username=your-email@gmail.com
SmtpSettings__Password=xxxx xxxx xxxx xxxx
SmtpSettings__FromEmail=your-email@gmail.com
SmtpSettings__FromName=Smart Campus
SmtpSettings__EnableSsl=true
```
**Not:** 
- Gmail App Password kullan (normal şifre değil)
- Google Account → Security → 2-Step Verification → App Passwords
- Eğer bu ayarları yapmazsan MockEmailService kullanılır (sadece console'a yazar)

## JWT Settings (Opsiyonel)
```
JwtSettings__Secret=SuperSecretKeyForSmartCampusProject_MustBeVeryLong_AtLeast32Chars
JwtSettings__Issuer=SmartCampusAPI
JwtSettings__Audience=SmartCampusClient
JwtSettings__AccessTokenExpirationMinutes=15
JwtSettings__RefreshTokenExpirationDays=7
```
**Not:** appsettings.json'daki varsayılanlar kullanılabilir, değiştirmene gerek yok.

## Migration Control (Opsiyonel)
```
SKIP_MIGRATIONS=true
```
**Not:** Migration'ı atlamak için (sadece acil durumlarda kullan)

## Önemli Notlar:
1. **PORT** environment variable Railway tarafından otomatik set edilir, elle ekleme.
2. **MySQL Connection:** Railway MySQL servisi eklendiğinde otomatik çalışır, elle bir şey yapmana gerek yok.
3. **SMTP Password:** Gmail App Password kullan (Google Account → Security → App Passwords)
4. **CORS:** `FRONTEND_URL` environment variable'ını kullan (virgülle ayrılmış birden fazla URL desteklenir)
5. Tüm `__` (double underscore) kullanımı .NET Configuration için gerekli.

