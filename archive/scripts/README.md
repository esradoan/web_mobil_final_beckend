# Archived Scripts

Bu klasör, artık aktif olarak kullanılmayan SQL ve PowerShell scriptlerini içerir.

## Dosyalar

### `part4_migration.sql`
- **Durum:** Artık kullanılmıyor
- **Neden:** SQL kodu `SmartCampus.API/Helpers/DbMigrationHelper.cs` içine gömülmüştür
- **Tarih:** 2025-12-27

### `fix_enrollment_studentid.sql`
- **Durum:** Tek seferlik düzeltme scripti, tamamlandı
- **Açıklama:** Enrollment.StudentId'nin UserId yerine Student.Id kullanması için düzeltme
- **Tarih:** 2025-12-27

### `fix_enrollment_studentid_complete.sql`
- **Durum:** Kullanılmamış versiyon
- **Açıklama:** Daha kapsamlı versiyon (foreign key constraint'leri de içeriyor)
- **Tarih:** 2025-12-27

### `fix_enrollment_studentid.ps1`
- **Durum:** PowerShell wrapper script, artık gerekli değil
- **Açıklama:** SQL script'ini çalıştırmak için kullanılan PowerShell script
- **Tarih:** 2025-12-27

## Not

Bu scriptler referans amaçlı saklanmaktadır. Gelecekte benzer sorunlar için örnek olarak kullanılabilir.

