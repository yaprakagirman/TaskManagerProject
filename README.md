# TaskManager API

> **Medyasoft — Vendorside Backend Ekibi Staj Projesi**

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EF%20Core-10.0.9-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://learn.microsoft.com/ef/core/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-18.4-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?style=for-the-badge&logo=docker&logoColor=white)](https://www.docker.com/)
[![Seq](https://img.shields.io/badge/Seq-Log%20Server-5C2D91?style=for-the-badge)](https://datalust.co/seq)
[![GitHub Actions](https://img.shields.io/badge/GitHub%20Actions-CI%2FCD-2088FF?style=for-the-badge&logo=githubactions&logoColor=white)](https://github.com/yaprakagirman/TaskManagerProject/actions)
[![Tests](https://img.shields.io/badge/Tests-147%20PASSED-brightgreen?style=for-the-badge&logo=checkmarx)](https://github.com/yaprakagirman/TaskManagerProject/actions)

---

## 1. Proje Özeti ve Amacı

**TaskManager API**, **Medyasoft Vendorside Backend Ekibi** bünyesindeki staj sürecinde geliştirilen; kullanıcı yönetimi, proje takibi, hiyerarşik görev yapılandırması, teknik yetkinlik bazlı çok kullanıcılı atama ve etiket yönetimini kapsayan kurumsal ölçekli bir **RESTful Web API** projesidir.

Proje; gerçek dünya yazılım geliştirme pratiklerini yansıtacak biçimde **Clean Architecture**, **SOLID prensipleri** ve birden fazla tasarım deseni ile inşa edilmiştir. Veritabanı bütünlüğü Soft Delete, Audit Trail ve Global Query Filter mekanizmalarıyla korunurken; uçtan uca test otomasyonu (Unit + Testcontainers Integration Tests), Rate Limiting, yapılandırılmış loglama (Serilog + Seq) ve GitHub Actions CI/CD pipeline ile kurumsal DevOps standartları sağlanmıştır.

### Temel Özellikler

- JWT Bearer Token kimlik doğrulama ve Rol Bazlı (`Admin`, `User`, `Manager`) yetkilendirme
- Bitwise bitmask ile uzmanlık bazlı görev atama doğrulaması (`[Flags] UserExpertise`)
- Özyinelemeli alt görev (subtask) hiyerarşisi ve döngü koruma algoritması
- Sayfalama, çoklu filtreleme, sıralama ve PostgreSQL `ILIKE` metin araması
- EF Core `ChangeTracker` ile otomatik Audit Trail ve Soft Delete koruması
- Global Exception Middleware ile RFC 7807 standartlı merkezi hata yönetimi
- Rate Limiting (100 istek/dakika), Health Check ve Swagger XML dokümantasyonu

---

## 2. Kullanılan Teknolojiler

| Katman | Teknoloji | Versiyon | Amaç |
| :--- | :--- | :---: | :--- |
| **Platform** | .NET / C# | `10.0` / `14` | Yüksek performanslı async web sunucusu |
| **ORM** | Entity Framework Core (Npgsql) | `10.0.9` | Code-First veritabanı yönetimi ve Migration |
| **Veritabanı** | PostgreSQL | `18.4` | İlişkisel veri depolama, ILIKE arama, kısmi indeks |
| **Kimlik Doğrulama** | JWT Bearer (HMAC-SHA256) | ASP.NET Core | Stateless token bazlı kimlik doğrulama |
| **Validasyon** | FluentValidation | `12.1.1` | DTO doğrulama kurallarının Controller'dan soyutlanması |
| **Nesne Eşleme** | AutoMapper | `16.2.0` | Entity ↔ DTO haritalama ve Flags enum dönüştürme |
| **Loglama** | Serilog + Seq | `10.0.0` | Yapılandırılmış JSON log üretimi ve merkezi takip |
| **Sağlık Kontrolü** | ASP.NET Health Checks | `10.0` | PostgreSQL bağlantısı ve latency ölçümü (`/health`) |
| **Güvenlik** | .NET Rate Limiter | `10.0` | Fixed Window (100 req/min), HTTP 429 koruması |
| **API Keşfi** | Swashbuckle (Swagger) | `10.2.3` | JWT Bearer destekli OpenAPI dokümantasyonu |
| **Container** | Docker + Docker Compose | — | API, PostgreSQL ve Seq servis orkestrasyonu |
| **Unit Test** | xUnit + Moq | — | Servis, Validator, Mapper ve Controller birim testleri |
| **Integration Test** | Testcontainers.PostgreSql | `4.13.0` | İzole Docker ortamında gerçek PostgreSQL testi |
| **CI/CD** | GitHub Actions | — | Otomatik derleme ve test pipeline'ı |

---

## 3. Mimari Yaklaşım ve Tasarım Desenleri

### 3.1 Clean Architecture

Bağımlılıkların **içeriye doğru** aktığı katmanlı mimari uygulanmıştır. Her katman yalnızca kendi içindeki veya iç katmanlardaki sınıflara bağımlıdır:

```
┌─────────────────────────────────────────────────────────────────────────┐
│  TaskManager.API                                                        │
│  Controller · Middleware · Rate Limiting · Swagger · Health Check       │
└────────────────────────┬────────────────────────────────────────────────┘
                         │  [Sadece Application Interfaces'e bağımlı]
          ┌──────────────▼──────────────┐
          │   TaskManager.Application   │
          │   DTO · Interface · Service │
          │   FluentValidation · Mapper │
          └──────────────┬──────────────┘
                         │  [Sadece Domain'e bağımlı]
          ┌──────────────▼──────────────┐       ┌──────────────────────────────┐
          │    TaskManager.Domain       │◄──────│  TaskManager.Infrastructure  │
          │  Entity · Enum · BaseClass  │       │  EF Core · Repo · JWT · Seq  │
          │  [Sıfır dış bağımlılık]     │       │  [Application'ı implement eder]
          └─────────────────────────────┘       └──────────────────────────────┘
```

> **SOLID Prensiplerinin Yansıması:**
> - **S** — Her sınıfın tek bir sorumluluğu var (`TaskAssignmentService` yalnızca atamayı, `TaskHierarchyService` yalnızca hiyerarşiyi yönetir).
> - **O** — Yeni repository tipi eklemek mevcut kodu değiştirmez (`IRepository<T>` genişletilebilir yapıda).
> - **L** — `FullAuditedEntityBase` türev sınıfları birbirinin yerine kullanılabilir.
> - **I** — `ITaskQueryRepository` ve `ITaskDetailRepository` özelleştirilmiş, küçük interface'lerdir.
> - **D** — Application katmanı EF Core'a değil `IRepository<T>` soyutlamasına bağımlıdır.

### 3.2 Uygulanan Tasarım Desenleri

| Desen | Uygulandığı Yer | Açıklama |
| :--- | :--- | :--- |
| **Composite Pattern** | `TaskItem.ParentTaskId → Subtasks` | Görevler kendi kendine referans alarak sonsuz derinlikte ağaç yapısı oluşturabilir (`GET /api/tasks/{id}/tree`) |
| **Bitmask / Flags Pattern** | `UserExpertise [Flags]` | Uzmanlıklar tek integer sütununda tutulur; `(user & required) == required` bitwise kontrolü ile eşleştirme yapılır |
| **Interceptor Pattern** | `AppDbContext.SaveChangesAsync` | ChangeTracker üzerinde Soft Delete ve Audit alanlarının otomatik yönetimi |
| **Repository + UoW Pattern** | `IRepository<T>` + `IUnitOfWork` | Veri erişim soyutlaması ve atomik transaction yönetimi |
| **Global Query Filter** | `HasQueryFilter(x => !x.IsDeleted)` | Silinmiş kayıtlar tüm sorguların dışında tutulur; Admin için `.IgnoreQueryFilters()` |
| **Chain of Responsibility** | ASP.NET Core Middleware Pipeline | GlobalException → RateLimiter → Authentication → Authorization → Controller |
| **Döngü Koruma (DFS)** | `TaskHierarchyService` | `HashSet<int>` ile derinlik öncelikli traversal; circular dependency tespiti ve `ConflictException` fırlatma |
| **Null Object Pattern** | `ICurrentUserService` | Kullanıcı kimliği `null` ise `UnauthorizedException` fırlatır; null kontrol kirliliği önlenir |

---

## 4. Öne Çıkan Kurumsal Özellikler

### 4.1 JWT Rol ve Bitmask Uzmanlık Yetkilendirmesi

**RBAC (Rol Bazlı Erişim Kontrolü):**

| Endpoint | Admin | Manager | User | Anonymous |
| :--- | :---: | :---: | :---: | :---: |
| `GET /api/tasks/deleted` | ✅ | ❌ 403 | ❌ 403 | ❌ 401 |
| `PATCH /api/tasks/{id}/restore` | ✅ | ❌ 403 | ❌ 403 | ❌ 401 |
| `DELETE /api/tasks/{taskId}/assignments/{userId}` | ✅ | ❌ 403 | ❌ 403 | ❌ 401 |
| `PATCH /api/tasks/{taskId}/assignments/transfer` | ✅ | ❌ 403 | ❌ 403 | ❌ 401 |
| `GET /api/users` | ✅ | ❌ 403 | ❌ 403 | ❌ 401 |
| `PATCH /api/users/{id}/role` | ✅ | ❌ 403 | ❌ 403 | ❌ 401 |

**ABAC (Öznitelik Bazlı — Bitwise Uzmanlık Doğrulaması):**

Bir göreve kullanıcı atanırken, göreve bağlı etiketlerin `RequiredExpertise` alanları OR operatörüyle birleştirilir ve kullanıcının uzmanlıklarıyla AND işlemine sokulur:

```csharp
// TaskAssignmentService.cs
private static bool HasAllRequiredExpertises(
    UserExpertise userExpertises,
    UserExpertise requiredExpertises)
{
    return (userExpertises & requiredExpertises) == requiredExpertises;
}
```

```
Örnek: Backend (0001) + QA (0100) = gereken uzmanlık → 0101
FullStack kullanıcı: Backend (0001) | Frontend (0010) = 0011
Kontrol: 0011 & 0101 = 0001 ≠ 0101 → ❌ REDDEDILDI (QA eksik)
```

Yetersiz uzmanlıkta sistem eksik uzmanlıkları listeleyerek `400 Bad Request` fırlatır.

---

### 4.2 Gelişmiş Sorgulama — Sayfalama, Filtreleme, Arama, Sıralama

`GET /api/tasks` uç noktası tam özellikli bir sorgu motoru sunar:

| Parametre | Tip | Açıklama |
| :--- | :---: | :--- |
| `page` / `pageSize` | `int` | Bellek dostu sunucu taraflı sayfalama (`Skip/Take`) |
| `status` | `enum` | `Pending`, `InProgress`, `Completed`, `Canceled` filtresi |
| `priority` | `enum` | `Low`, `Medium`, `High`, `Urgent` filtresi |
| `projectId` / `tagId` | `int` | İlişkisel filtreler |
| `search` | `string` | PostgreSQL `ILIKE` ile Türkçe uyumlu büyük/küçük harf duyarsız metin araması |
| `sortBy` / `sortDirection` | `string` | Sunucu taraflı dinamik sıralama |

```csharp
// TaskQueryRepository.cs — PostgreSQL ILIKE
.Where(t => EF.Functions.ILike(t.Title, $"%{request.Search}%"))
```

Yanıt modeli `PagedResponse<TaskResponse>` içerir:
`TotalCount`, `TotalPages`, `CurrentPage`, `HasNextPage`, `HasPreviousPage`.

---

### 4.3 Otomatik Audit Trail & Soft Delete

`AppDbContext.SaveChangesAsync` override edilerek ChangeTracker interceptor kurulmuştur:

```csharp
case EntityState.Added:
    entry.Entity.CreatedDate = currentDate;
    entry.Entity.CreatorId ??= currentUserId;
    break;

case EntityState.Modified:
    entry.Entity.LastModifiedDate = currentDate;
    entry.Entity.LastModifierId = currentUserId;
    break;

case EntityState.Deleted:           // ← Soft Delete dönüşümü
    entry.State = EntityState.Modified;
    entry.Entity.IsDeleted = true;
    entry.Entity.DeletedDate = currentDate;
    entry.Entity.DeleterId = currentUserId;
    break;
```

**Kısmi Benzersiz İndeks (Partial Unique Index):** Silinen bir kullanıcının e-posta adresiyle yeni kayıt oluşturulabilmesi için:

```sql
CREATE UNIQUE INDEX ON "Users" ("Email") WHERE "IsDeleted" = FALSE;
```

---

### 4.4 Global Exception Handling & Yapılandırılmış Loglama

`GlobalExceptionMiddleware` tüm uç noktalarda try-catch kirliliğini ortadan kaldırır. `AppException` türevleri HTTP durum koduna otomatik eşlenir; beklenmedik hatalar detay sızdırmadan `500` ile kapatılır:

```json
{
  "statusCode": 400,
  "message": "The user does not have the required task expertises: QA.",
  "path": "/api/tasks/15/assign",
  "traceId": "00-4d8a1b2c3e4f5a6b-7c8d9e0f1a2b3c4d-00"
}
```

Serilog logları yapılandırılmış JSON olarak Docker Compose `seq` servisine akar (`http://localhost:5341`). Seq arayüzünde `StatusCode >= 400 AND Application = 'TaskManager.API'` gibi zengin sorgular atılabilir.

---

### 4.5 Zenginleştirilmiş Health Check & Rate Limiting

**Health Check:** `GET /health` — PostgreSQL bağlantı durumu ve milisaniye bazlı gecikme:

```json
{
  "status": "Healthy",
  "timestamp": "2026-08-19T18:39:05Z",
  "totalDurationMs": 15.6,
  "entries": [
    {
      "key": "postgresql",
      "status": "Healthy",
      "description": "PostgreSQL is reachable.",
      "durationMs": 14.2
    }
  ]
}
```

**Rate Limiting:** ASP.NET Core yerleşik Fixed Window limiter — istemci başına dakikada maksimum 100 istek; aşımda `HTTP 429 Too Many Requests`.

---

## 5. Test Otomasyonu ve DevSecOps

### 5.1 Test Metrikleri

| Test Süiti | Başarılı | Başarısız | Toplam | Araç |
| :--- | :---: | :---: | :---: | :--- |
| **Birim Testleri** | 136 | 0 | 136 | xUnit + Moq |
| **Entegrasyon Testleri** | 11 | 0 | 11 | Testcontainers + PostgreSQL 18.4 |
| **TOPLAM** | **147** | **0** | **147** | — |

**Birim Testi Kapsayıcılığı:** Controller, Service (Auth, User, Task, TaskAssignment, TaskHierarchy, Tag), Validator (17 validator), AutoMapper profili, Middleware ve `AppDbContext` davranışları.

**Entegrasyon Testi Senaryoları:**
- Sayfalama, filtreleme, arama ve sıralama doğrulaması
- `GET /api/tasks/my-tasks` — JWT claim'inden kullanıcı kimliği çözme
- Admin-only silinen görev listeleme ve geri yükleme
- Atama silme: Anonymous → 401, User → 403, Admin → 204
- Atama transferi: Atomik swap ve uzmanlık reddi kontrolü
- `GET /health` — PostgreSQL bağlantı doğrulaması

### 5.2 Testcontainers ile İzole Test Ortamı

Testler EF Core InMemory yerine Docker üzerinde anlık ayağa kalkan gerçek PostgreSQL 18.4 container'ında koşturulur. Bu yaklaşım:
- `ILIKE` gibi PostgreSQL özgü sorguları doğrular
- Migration, kısıtlama ve indeks davranışlarını birebir test eder
- Geliştirme veritabanına hiç dokunmaz — her test çalışması tamamen izoledir

### 5.3 GitHub Actions CI/CD Pipeline

`.github/workflows/ci.yml` — `main` / `master` dalına `push` veya `pull_request` açıldığında otomatik tetiklenir:

```yaml
jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - Checkout Code          (actions/checkout@v4)
      - Setup .NET 10 SDK      (actions/setup-dotnet@v4)
      - dotnet restore
      - dotnet build --no-restore -c Release
      - dotnet test (Unit Tests)
      - dotnet test (Integration Tests)
```

### 5.4 Güvenlik Açığı Yaması (DevSecOps)

`Testcontainers.PostgreSql 4.13.0` bağımlılığından gelen `SSH.NET 2025.1.0` paketinde yüksek önem dereceli güvenlik zafiyeti (`CVE-2026-48798`, NU1903) tespit edilmiştir. Sorun, `TaskManager.API.IntegrationTests.csproj` dosyasına doğrudan `SSH.NET 2026.0.0` referansı eklenerek yamalanmıştır.

---

## 6. Kurulum ve Çalıştırma

### Ön Gereksinimler

| Araç | Minimum Versiyon |
| :--- | :---: |
| [.NET SDK](https://dotnet.microsoft.com/download) | `10.0` |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | `4.0+` |
| [dotnet-ef](https://learn.microsoft.com/ef/core/cli/dotnet) | `10.0` |

### 6.1 Ortam Değişkenleri

Proje kök dizininde `.env.example` dosyasını `.env` olarak kopyalayın ve değerleri doldurun:

```env
POSTGRES_USER=postgres
POSTGRES_PASSWORD=<güçlü-şifre>
POSTGRES_DB=TaskManagerDb
JWT_SECRET=<en-az-32-karakter-gizli-anahtar>
```

> ⚠️ `.env` dosyasını asla kaynak koduna eklemeyin — `.gitignore` tarafından zaten dışlanmıştır.

### 6.2 Docker Compose ile Tüm Sistemi Başlatma

```bash
# Seq external volume'ünü bir kez oluşturun:
docker volume create taskmanager-seq-data

# API + PostgreSQL + Seq servislerini başlatın:
docker compose up -d --build
```

| Servis | Adres |
| :--- | :--- |
| **API Swagger UI** | `http://localhost:8080/swagger` |
| **Health Check** | `http://localhost:8080/health` |
| **Seq Log Sunucusu** | `http://localhost:5341` |
| **PostgreSQL** | `localhost:5432` |

### 6.3 Yerel (CLI) Geliştirme Ortamı

```bash
# 1. Yalnızca veritabanını başlatın:
docker compose up -d postgres

# 2. Bağımlılıkları yükleyin ve derleyin:
dotnet restore TaskManager.slnx
dotnet build TaskManager.slnx

# 3. API'yi çalıştırın (.env proje kök dizininde olmalıdır):
dotnet run --project TaskManager.API/TaskManager.API.csproj
```

| Servis | Adres |
| :--- | :--- |
| **API Swagger UI** | `http://localhost:5177/swagger` |
| **Health Check** | `http://localhost:5177/health` |

### 6.4 Testleri Çalıştırma

```bash
# Birim testleri:
dotnet test TaskManager.Application.Tests/TaskManager.Application.Tests.csproj

# Entegrasyon testleri (Docker Desktop açık olmalıdır):
dotnet test TaskManager.API.IntegrationTests/TaskManager.API.IntegrationTests.csproj
```

### 6.5 Migration Yönetimi

```bash
# Mevcut migration'ları listele:
dotnet ef migrations list \
  --project TaskManager.Infrastructure/TaskManager.Infrastructure.csproj \
  --startup-project TaskManager.API/TaskManager.API.csproj

# Veritabanını güncelle:
dotnet ef database update \
  --project TaskManager.Infrastructure/TaskManager.Infrastructure.csproj \
  --startup-project TaskManager.API/TaskManager.API.csproj

# Yeni migration ekle:
dotnet ef migrations add <MigrationAdi> \
  --project TaskManager.Infrastructure/TaskManager.Infrastructure.csproj \
  --startup-project TaskManager.API/TaskManager.API.csproj
```

---

## 7. Proje Yapısı

```
TaskManagerProject/
├── .github/
│   └── workflows/ci.yml              ← GitHub Actions CI/CD pipeline
├── TaskManager.Domain/               ← Çekirdek varlıklar, enum'lar, base class'lar (sıfır bağımlılık)
│   ├── Common/BaseEntity.cs
│   ├── Common/FullAuditedEntityBase.cs
│   ├── Entities/                     ← User, Project, TaskItem, Tag, TaskAssignment, TaskTag
│   └── Enums/                        ← UserRole, UserExpertise [Flags], TaskItemStatus, TaskPriority
├── TaskManager.Application/          ← Uygulama servisleri, DTO'lar, interface'ler, validator'lar
│   ├── Common/Exceptions/            ← AppException, BadRequest, NotFound, Conflict, Unauthorized
│   ├── DTOs/                         ← 30+ DTO ve PagedResponse<T>
│   ├── Interfaces/                   ← IRepository<T>, IUnitOfWork, ICurrentUserService ve servis port'ları
│   ├── Mappings/MappingProfile.cs    ← AutoMapper; Flags enum → List<UserExpertise> dönüşümü
│   ├── Services/                     ← Auth, User, Project, Task, TaskAssignment, TaskHierarchy, Tag, TaskTag
│   └── Validators/                   ← 17 FluentValidation sınıfı
├── TaskManager.Infrastructure/       ← EF Core, repository'ler, JWT, migration'lar
│   ├── Persistence/AppDbContext.cs   ← SaveChanges interceptor: Audit + Soft Delete otomasyonu
│   ├── Persistence/Configurations/  ← 6 EF entity konfigürasyonu + Global Query Filters + Partial Index
│   ├── Repositories/                 ← Repository<T>, TaskDetailRepository, TaskQueryRepository
│   ├── Services/                     ← JwtTokenService, PasswordHasherService
│   └── Migrations/                   ← 10 EF Core migration (şema versiyonlama)
├── TaskManager.API/                  ← HTTP katmanı
│   ├── Controllers/                  ← Auth, Users, Projects, Tasks, Tags, TaskTags (+ BaseApiController)
│   ├── Configuration/                ← DevelopmentEnvironmentConfiguration (.env loader)
│   ├── HealthChecks/                 ← PostgreSqlHealthCheck
│   ├── Middlewares/                  ← GlobalExceptionMiddleware (RFC 7807)
│   ├── Services/CurrentUserService.cs
│   ├── Dockerfile                    ← Multi-stage Docker build
│   └── Program.cs                    ← DI, Rate Limiting, JWT, Swagger, Health Check kayıtları
├── TaskManager.Application.Tests/    ← 136 birim testi (xUnit + Moq)
├── TaskManager.API.IntegrationTests/ ← 11 entegrasyon testi (Testcontainers + PostgreSQL 18.4)
├── docker-compose.yml                ← API + PostgreSQL + Seq orkestrasyonu
└── TaskManager.slnx                  ← Solution tanımı
```

---

## 8. Hazır Test Kullanıcıları

Aşağıdaki kullanıcılar veritabanına seed edilmiştir. Swagger üzerinden `POST /api/auth/login` ile token alınabilir.

> **Ortak Şifre:** `TestPassword123!`

| Kullanıcı | E-Posta | Rol | Uzmanlıklar | Test Amacı |
| :--- | :--- | :---: | :--- | :--- |
| Admin User | `admin@test.com` | `Admin` | Backend, Frontend, QA, DevOps | Admin yetkili tüm işlemler |
| Manager User | `manager@test.com` | `Manager` | Backend, Frontend, QA, DevOps | Manager rol davranışı |
| Backend User | `backend@test.com` | `User` | Backend | Tekil Backend uzmanlık ataması |
| Frontend User | `frontend@test.com` | `User` | Frontend | Tekil Frontend uzmanlık ataması |
| QA User | `qa@test.com` | `User` | QA | Tekil QA uzmanlık ataması |
| DevOps User | `devops@test.com` | `User` | DevOps | Tekil DevOps uzmanlık ataması |
| FullStack User | `fullstack@test.com` | `User` | Backend, Frontend | Çoklu uzmanlık eşleştirme |
| Standard User | `user@test.com` | `User` | Yok (None) | Uzmanlık yetersizliği reddi testi |

---

*© 2026 — Medyasoft Vendorside Backend Ekibi Staj Projesi. All Rights Reserved.*
