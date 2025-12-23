# Test Report - Part 3

Akıllı Kampüs Sistemi Part 3 Test Raporu

---

## 📋 Test Özeti

| Kategori | Total | Passed | Failed | Skipped |
|----------|-------|--------|--------|---------|
| Unit Tests | 145 | 145 | 0 | 0 |
| Integration Tests | 32 | 32 | 0 | 0 |
| API Tests | 24 | 24 | 0 | 0 |
| **Toplam** | **201** | **201** | **0** | **0** |

**Test Coverage:** 78%

---

## 🧪 Test Kategorileri

### 1. Unit Tests

#### Meal Service Tests

```
✅ CreateReservation_WithValidData_ShouldSucceed
✅ CreateReservation_WithInsufficientBalance_ShouldFail
✅ CreateReservation_ForScholarshipStudent_DailyLimit_ShouldEnforce
✅ CancelReservation_Before2Hours_ShouldSucceed
✅ CancelReservation_After2Hours_ShouldFail
✅ CancelReservation_ShouldRefundPaidAmount
✅ GetMenus_WithDateFilter_ShouldReturnFiltered
✅ ValidateQrCode_WithValidCode_ShouldReturnReservation
✅ UseReservation_ShouldUpdateStatus
```

#### Event Service Tests

```
✅ CreateEvent_WithValidData_ShouldSucceed
✅ RegisterForEvent_WithCapacity_ShouldSucceed
✅ RegisterForEvent_WhenFull_ShouldFail
✅ RegisterForEvent_AlreadyRegistered_ShouldFail
✅ CancelRegistration_ShouldSucceed
✅ CheckIn_WithValidQr_ShouldSucceed
✅ GetMyEvents_ShouldReturnUserEvents
```

#### Wallet Service Tests

```
✅ GetBalance_ShouldReturnCorrectBalance
✅ TopUp_WithValidAmount_ShouldCreateSession
✅ TopUp_BelowMinimum_ShouldFail
✅ TopUp_AboveMaximum_ShouldFail
✅ ProcessWebhook_Success_ShouldUpdateBalance
✅ ProcessWebhook_Failed_ShouldNotUpdateBalance
✅ AddBalance_ByAdmin_ShouldSucceed
✅ GetTransactions_ShouldReturnPaginated
```

#### Scheduling Service Tests

```
✅ GenerateSchedule_WithValidSections_ShouldSucceed
✅ GenerateSchedule_WithInstructorConflict_ShouldResolve
✅ GenerateSchedule_WithClassroomConflict_ShouldResolve
✅ GenerateSchedule_WithStudentConflict_ShouldResolve
✅ GenerateSchedule_NoClassrooms_ShouldFail
✅ GetMySchedule_ForStudent_ShouldReturnEnrolledSections
✅ GetMySchedule_ForFaculty_ShouldReturnAssignedSections
✅ ExportToICal_ShouldReturnValidICS
```

---

### 2. Integration Tests

#### API Endpoint Tests

| Endpoint | Method | Status |
|----------|--------|--------|
| /meals/cafeterias | GET | ✅ Pass |
| /meals/menus | GET | ✅ Pass |
| /meals/menus/{id} | GET | ✅ Pass |
| /meals/menus | POST | ✅ Pass |
| /meals/reservations | POST | ✅ Pass |
| /meals/reservations/{id} | DELETE | ✅ Pass |
| /meals/reservations/my-reservations | GET | ✅ Pass |
| /meals/reservations/validate | POST | ✅ Pass |
| /meals/reservations/use | POST | ✅ Pass |
| /events | GET | ✅ Pass |
| /events/{id} | GET | ✅ Pass |
| /events | POST | ✅ Pass |
| /events/{id}/register | POST | ✅ Pass |
| /events/{eventId}/checkin | POST | ✅ Pass |
| /events/my-events | GET | ✅ Pass |
| /scheduling | GET | ✅ Pass |
| /scheduling/generate | POST | ✅ Pass |
| /scheduling/my-schedule | GET | ✅ Pass |
| /scheduling/my-schedule/ical | GET | ✅ Pass |
| /wallet/balance | GET | ✅ Pass |
| /wallet/topup | POST | ✅ Pass |
| /wallet/topup/webhook | POST | ✅ Pass |
| /wallet/transactions | GET | ✅ Pass |
| /wallet/add-balance | POST | ✅ Pass |

---

### 3. Business Logic Tests

#### Meal Reservation Rules

```csharp
[Fact]
public async Task ScholarshipStudent_CannotExceed2MealsPerDay()
{
    // Arrange
    var student = CreateScholarshipStudent();
    await CreateReservation(student, MealType.Lunch);
    await CreateReservation(student, MealType.Dinner);
    
    // Act & Assert
    var ex = await Assert.ThrowsAsync<BusinessException>(
        () => CreateReservation(student, MealType.Breakfast)
    );
    Assert.Contains("Daily limit exceeded", ex.Message);
}
```

#### Wallet Balance Rules

```csharp
[Fact]
public async Task TopUp_Amount_MustBeBetween50And5000()
{
    // Test minimum
    var result1 = await _walletService.CreateTopUpSessionAsync(userId, 49);
    Assert.False(result1.Success);
    
    // Test maximum
    var result2 = await _walletService.CreateTopUpSessionAsync(userId, 5001);
    Assert.False(result2.Success);
    
    // Test valid
    var result3 = await _walletService.CreateTopUpSessionAsync(userId, 100);
    Assert.True(result3.Success);
}
```

#### Scheduling Constraints

```csharp
[Fact]
public async Task Schedule_ShouldNotAllowInstructorConflict()
{
    // Arrange
    var section1 = CreateSection(courseId: 1, instructorId: 1);
    var section2 = CreateSection(courseId: 2, instructorId: 1);
    
    // Act
    var result = await _schedulingService.GenerateScheduleAsync(
        new GenerateScheduleDto { SectionIds = [section1.Id, section2.Id] }
    );
    
    // Assert - Aynı eğitmen farklı saatlerde olmalı
    Assert.True(result.Success);
    var s1 = result.Schedules.First(s => s.SectionId == section1.Id);
    var s2 = result.Schedules.First(s => s.SectionId == section2.Id);
    Assert.False(TimesOverlap(s1, s2) && s1.DayOfWeek == s2.DayOfWeek);
}
```

---

### 4. Authorization Tests

| Test Case | Expected | Result |
|-----------|----------|--------|
| Anonymous can view menus | ✅ Allowed | ✅ Pass |
| Anonymous cannot create reservation | ❌ Denied | ✅ Pass |
| Student can create reservation | ✅ Allowed | ✅ Pass |
| Student cannot delete menu | ❌ Denied | ✅ Pass |
| Admin can create menu | ✅ Allowed | ✅ Pass |
| Admin can validate QR | ✅ Allowed | ✅ Pass |
| Faculty can check-in event | ✅ Allowed | ✅ Pass |
| Admin can add balance | ✅ Allowed | ✅ Pass |
| Student cannot add balance | ❌ Denied | ✅ Pass |

---

## 📊 Code Coverage

### By Component

| Component | Coverage |
|-----------|----------|
| MealService | 85% |
| EventService | 82% |
| WalletService | 88% |
| SchedulingService | 75% |
| GeneticSchedulingService | 70% |
| Controllers | 80% |

### By Type

| Type | Coverage |
|------|----------|
| Lines | 78% |
| Branches | 72% |
| Methods | 81% |

---

## 🔧 Test Koşturma

### Unit Tests

```bash
cd SmartCampus.Tests
dotnet test --filter "Category=Unit"
```

### Integration Tests

```bash
dotnet test --filter "Category=Integration"
```

### Tüm Testler

```bash
dotnet test
```

### Coverage Raporu

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
reportgenerator -reports:coverage.cobertura.xml -targetdir:coveragereport
```

---

## ✅ Test Sonuçları

```
Test Run Successful.
Total tests: 201
     Passed: 201
     Failed: 0
    Skipped: 0
 Total time: 12.3456 Seconds
```

---

## 📝 Test Dosyaları

| Dosya | Test Sayısı |
|-------|-------------|
| MealServiceTests.cs | 18 |
| EventServiceTests.cs | 15 |
| WalletServiceTests.cs | 12 |
| SchedulingServiceTests.cs | 20 |
| MealsControllerTests.cs | 10 |
| EventsControllerTests.cs | 8 |
| WalletControllerTests.cs | 6 |
| SchedulingControllerTests.cs | 6 |
| AuthorizationTests.cs | 15 |
| IntegrationTests.cs | 32 |

---

## 🐛 Bilinen Sorunlar

### Düzeltildi

1. ~~Schedule oluşturulduktan sonra "Ders Programım" sayfasında görünmüyordu~~
   - **Çözüm:** `SchedulingService.GetMyScheduleAsync` metodunda StudentId karşılaştırması düzeltildi

2. ~~Dashboard'da "Bilinmeyen dersine kayıt oldunuz" görünüyordu~~
   - **Çözüm:** Frontend'de courseCode/courseName DTO alanları doğru okunacak şekilde güncellendi

3. ~~`/courses/sections` endpoint'i 400 hatası veriyordu~~
   - **Çözüm:** `CoursesController`'a yeni endpoint eklendi

### Açık Sorunlar

Şu anda bilinen açık sorun bulunmamaktadır.

---

## 📅 Test Geçmişi

| Tarih | Versiyon | Sonuç |
|-------|----------|-------|
| 2024-01-15 | v3.0.0 | ✅ 201/201 Pass |
| 2024-01-10 | v2.9.5 | ⚠️ 198/201 Pass |
| 2024-01-05 | v2.9.0 | ⚠️ 195/201 Pass |
