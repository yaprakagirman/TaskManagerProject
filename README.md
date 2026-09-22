# TaskManager API

Kullanıcı, proje, görev, alt görev, atama ve etiket yönetimi için geliştirilmiş .NET 10 REST API. Medyasoft Vendorside Backend Ekibi staj projesidir.

## Özellikler

- Kayıt ve giriş için JWT kimlik doğrulaması; yönetim işlemlerinde `Admin` rolü kontrolü
- Projeler, hiyerarşik görevler ve görev durumları
- Bir göreve birden fazla kullanıcı atama; etiketlerin gerektirdiği uzmanlıklarla uygunluk kontrolü
- Görevlerde sayfalama, filtreleme, arama ve sıralama; `my-tasks` görünümü
- Audit alanları, soft delete ve silinen görevleri geri yükleme
- PostgreSQL sağlık kontrolü (`/health`), Swagger UI ve Serilog/Seq loglama

## Teknolojiler ve yapı

Proje .NET 10, ASP.NET Core, Entity Framework Core, PostgreSQL, FluentValidation ve AutoMapper kullanır. Katmanlar:

| Proje | Sorumluluk |
| --- | --- |
| `TaskManager.Domain` | Varlıklar ve enum'lar |
| `TaskManager.Application` | İş kuralları, servisler, DTO'lar ve doğrulama |
| `TaskManager.Infrastructure` | EF Core, repository'ler, migration'lar, parola ve JWT servisleri |
| `TaskManager.API` | HTTP endpoint'leri, kimlik doğrulama, hata yönetimi ve Swagger |
| `TaskManager.Application.Tests` | Birim testleri |
| `TaskManager.API.IntegrationTests` | Testcontainers ile PostgreSQL entegrasyon testleri |

## Hızlı başlangıç

Gereksinimler: .NET 10 SDK ve Docker Desktop. Entegrasyon testleri de çalışan bir Docker engine gerektirir.

1. `.env.example` dosyasını proje kökünde `.env` olarak kopyalayın. `POSTGRES_PASSWORD` ve en az 32 karakterlik `JWT_SECRET` için kendi değerlerinizi girin. `.env` Git tarafından yok sayılır.
2. Seq veri alanını oluşturup servisleri başlatın:

   ```bash
   docker volume create taskmanager-seq-data
   docker compose up -d --build
   ```

3. Veritabanı migration'larını uygulayın. Bunun için .NET EF aracının 10.x sürümü gerekir:

   ```bash
   dotnet ef database update --project TaskManager.Infrastructure/TaskManager.Infrastructure.csproj --startup-project TaskManager.API/TaskManager.API.csproj
   ```

API: `http://localhost:8080` · Swagger: `http://localhost:8080/swagger` · Sağlık kontrolü: `http://localhost:8080/health` · Seq: `http://localhost:5341`.

Yerel geliştirme için `docker compose up -d postgres` komutuyla yalnızca veritabanını açıp `dotnet run --project TaskManager.API/TaskManager.API.csproj --launch-profile http` çalıştırabilirsiniz. Bu profilde Swagger adresi `http://localhost:5177/swagger` olur. `.env` kök dizinde bulunmalıdır.

## API kullanımı

Veritabanına otomatik demo kullanıcı eklenmez. Önce `POST /api/auth/register` ile hesap oluşturun, ardından `POST /api/auth/login` ile JWT alın. Swagger'daki **Authorize** düğmesine yalnızca token değerini girin.

Başlıca uç noktalar:

| Alan | Uç noktalar |
| --- | --- |
| Kimlik doğrulama | `POST /api/auth/register`, `POST /api/auth/login` |
| Kullanıcılar | `GET /api/users/me`, `GET /api/users/{id}`, `GET /api/users` (Admin), `PATCH /api/users/{id}/role` (Admin), `PATCH /api/users/{id}/expertises` (Admin) |
| Projeler | `GET /api/projects`, `GET /api/projects/{id}/tasks`, `POST /api/projects`, `PUT /api/projects/{id}` |
| Görevler | `GET /api/tasks`, `GET /api/tasks/my-tasks`, `GET /api/tasks/{id}/tree`, `POST /api/tasks`, `PUT /api/tasks/{id}`, `PUT /api/tasks/{id}/status`, `DELETE /api/tasks/{id}` |
| Atamalar | `POST /api/tasks/{taskId}/assign`, `GET /api/tasks/{taskId}/eligible-users`, atama silme ve transfer (Admin) |
| Silinen görevler | `GET /api/tasks/deleted`, `PATCH /api/tasks/{id}/restore` (Admin) |
| Etiketler | `/api/tags`, `/api/tasktags` |

`GET /api/tasks` sorgusunda `page`, `pageSize`, `search`, `status`, `priority`, `projectId`, `assignedUserId`, `tagId`, `sortBy` ve `sortDirection` kullanılabilir. Geçerli durumlar `Pending`, `InProgress`, `Completed`, `Cancelled`; öncelikler `Low`, `Medium`, `High`, `Critical` değerleridir. Ayrıntılı istek/yanıt şemaları Swagger'dadır.

## Testler

```bash
dotnet test TaskManager.Application.Tests/TaskManager.Application.Tests.csproj
dotnet test TaskManager.API.IntegrationTests/TaskManager.API.IntegrationTests.csproj
```

Birim testleri bu güncellemede 136/136 başarılıydı. Entegrasyon testleri yerel ortamda Docker engine çalışmadığı için tamamlanamadı; GitHub Actions akışı iki test projesini de çalıştıracak şekilde tanımlıdır.

## Yapılandırma ve güvenlik

Gizli değerleri depoya eklemeyin. `.env.example` örnek değerler içerir; gerçek `.env` dosyası `.gitignore` kapsamındadır. `TaskManager.API/appsettings.json` içindeki bağlantı ve JWT değerleri de yer tutucudur. Dağıtım ortamında bunları ortam değişkenleri veya güvenli bir gizli değer deposu ile sağlayın.
