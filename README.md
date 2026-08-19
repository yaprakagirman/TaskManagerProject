# TaskManager API

TaskManager; kullanıcı, proje, görev, alt görev, atama ve etiket ilişkilerini yöneten, JWT ile korunan bir ASP.NET Core Web API projesidir. Bu README, 29. gün itibarıyla kodda doğrulanan çalışma biçimini özetler.

## Özellikler

- Kayıt ve giriş işlemleri; JWT üretimi ve Bearer doğrulaması
- Kullanıcı listeleme, görüntüleme, oluşturma, güncelleme, silme ve rol güncelleme
- Proje listeleme, görüntüleme, oluşturma, güncelleme ve projeye bağlı görevleri listeleme
- Görev CRUD işlemleri, öncelik/durum yönetimi ve ayrıntılı görev görünümü
- Kullanıcıya görev atama
- Parent-child görev ilişkisi ve görev ağacı görüntüleme
- Etiket CRUD işlemleri ve görev-etiket ilişkisi yönetimi
- FluentValidation tabanlı istek doğrulama
- Merkezi hata sözleşmesi, TraceId ve Serilog/Seq loglama
- Swagger/OpenAPI dokümantasyonu
- PostgreSQL, EF Core migrationları ve Docker Compose altyapısı

> Durum ayrımı: Audit alanları şemada bulunur; ancak aktör kimliklerinin otomatik doldurulması ve soft delete sorgu filtreleri tamamlanmış değildir. Görev atamasında kaldırma/tamamlama akışı, proje silme API endpoint'i ve kaynak sahipliğine dayalı yetkilendirme de mevcut değildir.

## Teknolojiler

| Alan | Teknoloji |
|---|---|
| Platform | .NET 10 / ASP.NET Core Web API |
| Veri erişimi | Entity Framework Core 10.0.9 |
| Veritabanı | PostgreSQL; Docker imajı `postgres:18.4` |
| PostgreSQL sağlayıcısı | Npgsql.EntityFrameworkCore.PostgreSQL 10.0.2 |
| Kimlik doğrulama | JWT Bearer, HMAC-SHA256 |
| Mapping | AutoMapper 16.2.0 |
| Validation | FluentValidation 12.1.1 |
| Loglama | Serilog, Seq |
| API keşfi | Swashbuckle / Swagger |
| Test | xUnit, Moq, EF Core InMemory |

## Mimari

Solution katmanlı bir yapı kullanır:

```text
HTTP istemcisi
      |
TaskManager.API            Controller, middleware, Swagger, auth pipeline
      |
TaskManager.Application    DTO, validator, service, mapping, arayüzler
      |
TaskManager.Infrastructure EF Core, repository, JWT/parola servisleri
      |
TaskManager.Domain         Entity, enum ve ortak temel sınıflar
      |
PostgreSQL
```

## Kurulum

Gereksinimler:

- .NET 10 SDK
- PostgreSQL veya Docker Desktop
- Migration komutları için `dotnet-ef`

Gizli değerleri repository'ye yazmayın. Aşağıdaki değişkenleri güvenli bir secret store, CI/CD secret alanı veya yerel kullanıcı secret'ı üzerinden sağlayın:

```text
Jwt__Key=${JWT_SECRET}
ConnectionStrings__DefaultConnection=${CONNECTION_STRING}
POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
```

Yerel çalıştırma:

```powershell
dotnet restore TaskManager.slnx
dotnet build TaskManager.slnx
dotnet run --project TaskManager.API/TaskManager.API.csproj
```

Varsayılan geliştirme profilleri:

- HTTP: `http://localhost:5177`
- HTTPS: `https://localhost:7285`

## Docker Compose

Compose dosyası `postgres`, `seq` ve `api` servislerini tanımlar. Güvenli örnek ortam değişkenleri:

```text
POSTGRES_USER=taskmanager
POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
POSTGRES_DB=TaskManagerDb
JWT_SECRET=${JWT_SECRET}
```

Mevcut Compose tanımı PostgreSQL değişkenlerini API connection string'ine aktarır; `Jwt__Key` için doğrudan bir environment eşlemesi içermez. Güvenli çalıştırma için commit edilmeyen bir Compose override dosyasında şu eşlemeyi ekleyin:

```yaml
services:
  api:
    environment:
      Jwt__Key: ${JWT_SECRET}
```

Ardından:

```powershell
docker compose up --build -d
docker compose ps
docker compose logs -f api
```

Docker üzerinden API `http://localhost:8080`, Seq arayüzü `http://localhost:5341` adresindedir. `seq-data` volume'ü external tanımlıdır; yoksa önce oluşturun:

```powershell
docker volume create taskmanager-seq-data
```

## Migration Komutları

Migration listesini görüntüleme:

```powershell
dotnet ef migrations list --project TaskManager.Infrastructure/TaskManager.Infrastructure.csproj --startup-project TaskManager.API/TaskManager.API.csproj --context AppDbContext
```

Veritabanını güncelleme:

```powershell
dotnet ef database update --project TaskManager.Infrastructure/TaskManager.Infrastructure.csproj --startup-project TaskManager.API/TaskManager.API.csproj --context AppDbContext
```

Yeni migration oluşturma:

```powershell
dotnet ef migrations add <MigrationAdi> --project TaskManager.Infrastructure/TaskManager.Infrastructure.csproj --startup-project TaskManager.API/TaskManager.API.csproj --context AppDbContext
```

Kodda 9 migration bulunmaktadır. Son migration aktif kayıtlar için kısmi benzersiz indeksleri ve aggregate ilişkilerindeki `Restrict` davranışını ekler.

## Test Komutları

```powershell
dotnet test TaskManager.Application.Tests/TaskManager.Application.Tests.csproj
```

Son doğrulama: **108 başarılı, 0 başarısız, 0 atlanan**.

## Swagger Kullanımı

- Yerel HTTP: `http://localhost:5177/swagger`
- Yerel HTTPS: `https://localhost:7285/swagger`
- Docker: `http://localhost:8080/swagger`

Korunan endpoint'lerde yalnızca geçerli JWT gerekir. Swagger'daki global **Authorize** düğmesine token bir kez girilir; `X-Client-Id` kullanılmaz.

## Authentication Örneği

Kayıt:

```http
POST /api/auth/register
Content-Type: application/json

{
  "firstName": "Demo",
  "lastName": "User",
  "email": "demo@example.test",
  "password": "${USER_PASSWORD}"
}
```

Yanıttaki token ile korunan çağrı:

```http
GET /api/tasks
Authorization: Bearer <JWT_TOKEN>
```

JWT; kullanıcı kimliği, e-posta, ad-soyad ve rol claim'lerini taşır. Kodda token süresi 60 dakika olarak yapılandırılmıştır.

## Proje Yapısı

```text
TaskManager.Domain/              Entity, enum ve audit temel sınıfları
TaskManager.Application/         DTO, validator, mapping, service ve portlar
TaskManager.Infrastructure/      EF Core, migration, repository ve güvenlik servisleri
TaskManager.API/                 Controller, middleware, Swagger ve uygulama başlangıcı
TaskManager.Application.Tests/   108 çalışan test vakası
TaskManager.API/Dockerfile       Çok aşamalı .NET container build'i
docker-compose.yml               API, PostgreSQL ve Seq orkestrasyonu
TaskManager.slnx                 Solution tanımı
```

## Bilinen Sınırlamalar ve Gelecek Geliştirmeler

### Kısmen uygulanmış

- Full audit alanları JWT'deki kullanıcı kimliğiyle merkezi olarak doldurulur.
- `User`, `Project`, `TaskItem` ve `Tag` soft delete edilir; normal sorgular global query filter ile silinmiş kayıtları gizler.
- Role-based authorization yalnızca kullanıcı listesini ve rol güncellemesini Admin rolüyle sınırlar.
- ProjectService proje silmeyi uygular; API controller'da karşılık gelen DELETE endpoint'i yoktur.
- TaskAssignment modeli tamamlanma alanları içerir; yalnızca atama oluşturma endpoint'i vardır.

### Henüz uygulanmamış

- Refresh token, parola sıfırlama, hesap kilitleme ve rate limiting
- Kaynak sahipliği/tenant bazlı yetkilendirme
- Atama kaldırma ve atamayı tamamlandı olarak işaretleme endpoint'leri
- Pagination, sıralama ve genel arama/filtreleme
- PostgreSQL ile uçtan uca/integration test paketi ve ölçülmüş coverage raporu
- Production ortamında Swagger'ı koşullu açma ve health-check endpoint'i

## Güvenlik Notları

- Build, `System.Security.Cryptography.Xml` 8.0.3 için yüksek önem dereceli bilinen açık uyarıları üretmektedir; paket güncellemesi değerlendirilmelidir.
- Mevcut ayarlarda literal JWT anahtarı tespit edilmiştir. Değer bu belgede gösterilmemiştir; `${JWT_SECRET}` ile dışarıdan verilmelidir.
- Swagger tüm ortamlarda açılır. Production için ortam koşulu önerilir.
- Kimlik doğrulama logları e-posta alanı içerir; kişisel veri ve log saklama politikası gözden geçirilmelidir.
