# TaskManager Swagger ve PostgreSQL Doğrulama Rehberi

Bu rehber yerel/geliştirme veritabanı içindir. Üretim verisinde silme deneyi yapmayın. Parola, JWT anahtarı ve bağlantı bilgisini belgeye veya ekran görüntüsüne koymayın.

## 1. Ortamı başlatma

```powershell
docker compose config --quiet
docker compose build
docker compose up -d postgres seq api
docker compose ps
```

- Swagger: `http://localhost:8080/swagger`
- Seq: `http://localhost:5341`
- PostgreSQL: `localhost:5432` (kullanıcı/parola/veritabanı adını yerel yapılandırmadan alın)
- Yerel `dotnet run` alternatifi: `http://localhost:5177/swagger`

Önce migration durumunu kontrol edin ve yalnızca doğru yerel veritabanına uygulayın:

```powershell
dotnet ef migrations list --project TaskManager.Infrastructure --startup-project TaskManager.API
dotnet ef database update --project TaskManager.Infrastructure --startup-project TaskManager.API
```

## 2. Kimlik doğrulama ve roller

### 2.1 Kayıt

`POST /api/auth/register` — Anonymous — beklenen `200 OK`

```json
{
  "firstName": "Demo",
  "lastName": "User",
  "email": "demo.unique@example.test",
  "password": "Secret123!"
}
```

Yanıt `userId`, `firstName`, `lastName`, `email`, `token` alanlarını taşır. PostgreSQL'de `Users` satırı oluşur; `CreatedDate` dolar, unauthenticated kayıt akışında `CreatorId` null olabilir. Aynı e-postayı tekrar gönderme negatif testi `409 Conflict` vermelidir.

### 2.2 Giriş ve Swagger Authorize

`POST /api/auth/login` — Anonymous — beklenen `200 OK`

```json
{
  "email": "demo.unique@example.test",
  "password": "Secret123!"
}
```

Yanıttaki JWT'yi kopyalayın. Swagger'da **Authorize** düğmesine bir kez basın ve yalnızca token değerini girin. Herhangi bir operasyonda `X-Client-Id` alanı görünmemelidir. Yanlış parola `401 Unauthorized` vermelidir.

### 2.3 Yetki negatif testleri

1. Swagger'da **Logout** yapın ve `GET /api/tasks` çağırın: `401`.
2. User rolündeki token ile `GET /api/users` çağırın: `403`.
3. Yeniden **Authorize** ile geçerli token'ı girin: korunan normal endpoint'ler çalışmalıdır.
4. Admin testi için mevcut bir Admin hesabı kullanın. Yerel veritabanında hiç Admin yoksa yalnızca test hesabını bir defa `Role = 2` yapıp tekrar login olun; mevcut token'ın rolü sonradan değişmez.

## 3. Örnek veri gövdeleri

`<...Id>` yerlerine önceki yanıtlardan alınan gerçek kimlikleri yazın. Enum değerleri: Role `User=1, Admin=2, Manager=3`; Priority `Low=1, Medium=2, High=3, Critical=4`; Status `Pending=1, InProgress=2, Completed=3, Cancelled=4`.

Kullanıcı oluşturma/güncelleme:

```json
{
  "firstName": "Test",
  "lastName": "Assignee",
  "email": "assignee.unique@example.test"
}
```

Proje oluşturma/güncelleme:

```json
{
  "name": "Staj Projesi",
  "description": "Swagger doğrulama projesi"
}
```

Ana task oluşturma:

```json
{
  "title": "API doğrulama görevi",
  "description": "PostgreSQL kalıcılık kontrolü",
  "priority": 3,
  "dueDate": null,
  "projectId": <projectId>,
  "parentTaskId": null
}
```

Alt task oluşturma:

```json
{
  "title": "Alt görev",
  "description": "Hiyerarşi kontrolü",
  "priority": 2,
  "dueDate": null,
  "projectId": <projectId>,
  "parentTaskId": <parentTaskId>
}
```

Task güncelleme:

```json
{
  "title": "API doğrulama görevi - güncel",
  "description": "Update ve audit kontrolü",
  "priority": 4,
  "dueDate": null
}
```

Atama ve durum:

```json
{ "assignedUserId": <userId> }
```

```json
{ "status": 2 }
```

Tag oluşturma/güncelleme:

```json
{ "name": " Backend " }
```

Task'ları tag'e bağlama:

```json
{ "taskIds": [<taskId>, <subtaskId>] }
```

TaskTag güncelleme:

```json
{ "tagId": <otherTagId> }
```

## 4. Endpoint envanteri ve beklenen davranış

| Endpoint | Ön koşul / kullanılacak ID | Başarı | Yanıt ve PostgreSQL etkisi | Önemli negatif test | Admin |
|---|---|---:|---|---|---:|
| `POST /api/auth/register` | Benzersiz e-posta; kayıt gövdesi | 200 | `AuthResponse`; `Users` INSERT, token üretilir | Aynı e-posta: 409; geçersiz gövde: 400 | Hayır |
| `POST /api/auth/login` | Kayıtlı aktif kullanıcı | 200 | `AuthResponse`; veri değişmez | Yanlış parola/silinmiş kullanıcı: 401 | Hayır |
| `GET /api/users` | Admin JWT | 200 | `UserResponse[]`; yalnızca aktif kullanıcılar | User token: 403; tokensız: 401 | Evet |
| `GET /api/users/{id}` | Gerçek `User.Id` | 200 | `UserResponse`; veri değişmez | Olmayan/silinmiş ID: 404 | Hayır |
| `POST /api/users` | JWT; kullanıcı gövdesi | 200 | `UserResponse`; `Users` INSERT, `CreatorId` JWT user ID | Aynı aktif e-posta: 409 | Hayır |
| `PUT /api/users/{id}` | Gerçek `User.Id`; kullanıcı gövdesi | 200 | `UserResponse`; UPDATE, `LastModifiedDate/LastModifierId` dolar | Olmayan/silinmiş ID: 404 | Hayır |
| `DELETE /api/users/{id}` | Gerçek `User.Id` | 204 | Gövde yok; satır kalır, soft-delete alanları dolar | İkinci silme: 404 | Hayır |
| `PATCH /api/users/{id}/role` | Admin JWT, gerçek `User.Id`, `{ "role": 2 }` | 200 | `UserResponse`; rol ve modification audit güncellenir | User token: 403; geçersiz enum: 400 | Evet |
| `GET /api/projects` | JWT | 200 | `ProjectResponse[]`; aktif projeler | Tokensız: 401 | Hayır |
| `GET /api/projects/{id}` | Gerçek `Project.Id` | 200 | `ProjectResponse` | Olmayan/silinmiş ID: 404 | Hayır |
| `GET /api/projects/{id}/tasks` | Gerçek `Project.Id` | 200 | `TaskResponse[]`; aktif task'lar | Olmayan proje: 404 | Hayır |
| `POST /api/projects` | JWT; proje gövdesi | 200 | `ProjectResponse`; `Projects` INSERT + create audit | Boş/uzun ad: 400 | Hayır |
| `PUT /api/projects/{id}` | Gerçek `Project.Id`; proje gövdesi | 200 | Boş başarı gövdesi; UPDATE + modification audit | Olmayan/silinmiş ID: 404 | Hayır |
| `GET /api/tasks` | JWT | 200 | `TaskResponse[]`; aktif task'lar | Tokensız: 401 | Hayır |
| `GET /api/tasks/{id}` | Gerçek `TaskItem.Id` | 200 | Detay; project, tag ve assignment listeleri | Olmayan/silinmiş ID: 404 | Hayır |
| `GET /api/tasks/{id}/tree` | Gerçek ana/alt `TaskItem.Id` | 200 | İç içe `TaskTreeResponse` | Olmayan/silinmiş ID: 404 | Hayır |
| `POST /api/tasks` | JWT; var olan project/parent ID | 200 | `TaskResponse`; `Tasks` INSERT, `CreatedByUserId` ve `CreatorId` JWT'den | Başka projedeki parent: 400; olmayan ID: 404 | Hayır |
| `PUT /api/tasks/{id}` | Gerçek `TaskItem.Id`; update gövdesi | 200 | `TaskResponse`; UPDATE + modification audit | Olmayan/silinmiş ID: 404 | Hayır |
| `DELETE /api/tasks/{id}` | Gerçek `TaskItem.Id` | 204 | Satır kalır; soft-delete alanları dolar | İkinci silme: 404 | Hayır |
| `POST /api/tasks/{taskId}/assign` | Gerçek `TaskItem.Id` ve `User.Id` | 200 | `TaskResponse`; `TaskAssignments` INSERT | Aynı atama: 409; olmayan user/task: 404 | Hayır |
| `PUT /api/tasks/{taskId}/status` | Gerçek `TaskItem.Id`; status gövdesi | 200 | `TaskResponse`; status ve modification audit güncellenir | Geçersiz enum: 400; olmayan task: 404 | Hayır |
| `GET /api/tags` | JWT | 200 | `TagResponse[]`; aktif tag'ler | Tokensız: 401 | Hayır |
| `GET /api/tags/{id}` | Gerçek `Tag.Id` | 200 | `TagResponse` | Olmayan/silinmiş ID: 404 | Hayır |
| `POST /api/tags` | JWT; tag gövdesi | 200 | `TagResponse`; normalize edilmiş adla INSERT + create audit | Aynı aktif ad: 409 | Hayır |
| `PUT /api/tags/{id}` | Gerçek `Tag.Id`; tag gövdesi | 204 | Gövde yok; UPDATE + modification audit | Olmayan/silinmiş ID: 404; duplicate: 409 | Hayır |
| `DELETE /api/tags/{id}` | **`GET /api/tags` içindeki `Tag.Id`** | 204 | Satır kalır; soft-delete alanları dolar | İkinci silme: 404 | Hayır |
| `GET /api/tasktags?taskId=&tagId=` | İsteğe bağlı gerçek `TaskItem.Id`/`Tag.Id` filtreleri | 200 | `TaskTagResponse[]`; `id` alanı ilişki kimliğidir | Olmayan filtre ID'si: boş liste | Hayır |
| `POST /api/tasktags/tags/{tagId}/tasks` | Gerçek `Tag.Id`; task ID listesi | 204 | Her yeni bağlantı için `TaskTags` INSERT | Olmayan tag/task: 404; tekrarlı body ID: 400 | Hayır |
| `GET /api/tasktags/{id}` | Gerçek **`TaskTag.Id`** | 200 | `TaskTagDetailResponse` | Olmayan/gizlenen ilişki: 404 | Hayır |
| `PUT /api/tasktags/{id}` | Gerçek **`TaskTag.Id`**, başka aktif `Tag.Id` | 200 | `TaskTagDetailResponse`; ilişkinin `TagId` değeri güncellenir | Duplicate ilişki: 409; olmayan tag: 404 | Hayır |
| `DELETE /api/tasktags/{taskTagId}` | **`GET /api/tasktags` içindeki `TaskTag.Id`** | 204 | İlişki satırı fiziksel DELETE edilir | İkinci silme: 404 | Hayır |

Not: `ProjectService` içinde silme davranışı olsa da güncel `ProjectsController` bir DELETE route'u sunmuyor. TaskAssignment kaldırma/tamamlama endpoint'i de yoktur; Swagger'da olmayan route uydurmayın.

## 5. Önerilen uçtan uca test sırası

1. PostgreSQL, Seq ve API'yi başlatın; container'ların healthy/running olduğunu doğrulayın.
2. Swagger'ı açın ve `POST /api/auth/register` ile benzersiz bir hesap oluşturun.
3. `POST /api/auth/login` çağrısını yapın, JWT'yi kopyalayın.
4. Swagger **Authorize** düğmesine token'ı bir kez girin; `X-Client-Id` bulunmadığını doğrulayın.
5. Logout ile `GET /api/tasks -> 401`, User token ile `GET /api/users -> 403` kontrollerini yapın.
6. Admin hesabıyla login/Authorize olun; `GET /api/users -> 200` kontrolünü yapın.
7. `POST /api/users` ile atama yapılacak kullanıcıyı oluşturun; yanıttaki `User.Id` değerini kaydedin.
8. `GET /api/users/{id}` ile okuyun; SQL ile aynı satırı doğrulayın.
9. `PUT /api/users/{id}` ve gerekirse Admin ile `PATCH /api/users/{id}/role` çağrılarını yapın; SQL'de modification audit'i doğrulayın.
10. `POST /api/projects` ile proje oluşturun; `Project.Id` değerini kaydedin. `GET /api/projects` ve `GET /api/projects/{id}` ile doğrulayın.
11. `POST /api/tasks` ile ana task oluşturun; `TaskItem.Id` değerini kaydedin. `GET /api/tasks/{id}` ve SQL ile doğrulayın.
12. `PUT /api/tasks/{id}` ile task'ı güncelleyin; GET ve SQL'de içerik/audit değişimini doğrulayın.
13. `PUT /api/tasks/{id}/status` ile status `2` yapın; GET ve SQL'de doğrulayın.
14. `POST /api/tasks/{id}/assign` ile 7. adımın `User.Id` değerini kullanın; SQL'de `TaskAssignments` satırını doğrulayın. Aynı isteği tekrarlayıp `409` görün.
15. Aynı `Project.Id` ve ana task'ın `TaskItem.Id` değeriyle alt task oluşturun. `GET /api/tasks/{parentId}/tree` ve `GET /api/projects/{projectId}/tasks` ile hiyerarşiyi doğrulayın.
16. Farklı proje ID'si + mevcut parent ID ile task oluşturarak `400` negatif testini yapın.
17. `POST /api/tags` ile iki tag oluşturun. Tag adının trim + lowercase döndüğünü ve `200` durumunu doğrulayın.
18. `GET /api/tags` çağrısından her iki gerçek **`Tag.Id`** değerini kaydedin.
19. `POST /api/tasktags/tags/{tagId}/tasks` ile ana ve alt task'ı ilk tag'e bağlayın; `204` bekleyin.
20. `GET /api/tasktags?tagId={tagId}` çağrısından gerçek **`TaskTag.Id`** değerlerini kaydedin. Bunların `Tag.Id` olmadığını özellikle not edin.
21. `GET /api/tasktags/{taskTagId}` ile detay alın; `PUT /api/tasktags/{taskTagId}` ile ikinci tag'e taşıyın ve SQL'de `TagId` değişimini doğrulayın.
22. `DELETE /api/tasktags/{taskTagId}` çağrısında **ilişki ID'sini** kullanın; `204`, listeden kaybolma ve SQL'de satırın gerçekten yok olması kontrollerini yapın. İkinci çağrı `404` olmalıdır.
23. Yeni bir TaskTag ilişkisi oluşturun. Ardından `DELETE /api/tags/{tagId}` çağrısında `GET /api/tags` yanıtındaki **tag ID'sini** kullanın; `204` bekleyin.
24. Silinen tag'in `GET /api/tags` ve `GET /api/tags/{id}` sonuçlarında görünmediğini doğrulayın.
25. SQL ile tag satırının hâlâ bulunduğunu, `IsDeleted=true`, `DeletedDate` dolu ve `DeleterId` token sahibinin ID'si olduğunu doğrulayın. İkinci Tag DELETE `404` olmalıdır.
26. Soft silinen tag adıyla tekrar `POST /api/tags` yapın: aktif kayda özel kısmi unique index sayesinde `200` olmalıdır. Aktif aynı adı tekrar gönderin: `409` olmalıdır.
27. Boş tag adı, geçmiş due date, `assignedUserId=0` ile `400`; olmayan pozitif ID'lerle `404`; duplicate e-posta/tag/atama ile `409` testlerini yapın.
28. Ayrı bir test task'ını `DELETE /api/tasks/{id}` ile silin; normal GET'te gizlendiğini ve SQL'de soft-delete audit alanlarını doğrulayın.
29. Ayrı bir test kullanıcısını `DELETE /api/users/{id}` ile silin; normal GET'te gizlendiğini ve SQL'de soft-delete audit alanlarını doğrulayın.
30. Seq'de ilgili zaman aralığını açın. Servis loglarında `UserId`, `TaskId`, `TagId` gibi yapılandırılmış alanları; hata HTTP gövdesinde `TraceId` değerini kontrol edin. Beklenmeyen hata üretmeden yalnızca güvenli negatif senaryoları kullanın.

## 6. PostgreSQL doğrulama sorguları

Parametreleri DBeaver/psql içinde gerçek test ID'leriyle değiştirin.

```sql
SELECT "Id", "Email", "Role", "CreatedDate", "CreatorId",
       "LastModifiedDate", "LastModifierId",
       "IsDeleted", "DeletedDate", "DeleterId"
FROM "Users"
ORDER BY "Id";

SELECT "Id", "Name", "Description", "CreatedDate", "CreatorId",
       "LastModifiedDate", "LastModifierId",
       "IsDeleted", "DeletedDate", "DeleterId"
FROM "Projects"
ORDER BY "Id";

SELECT "Id", "Title", "Status", "Priority", "ProjectId", "ParentTaskId",
       "CreatedByUserId", "CreatedDate", "CreatorId",
       "LastModifiedDate", "LastModifierId",
       "IsDeleted", "DeletedDate", "DeleterId"
FROM "Tasks"
ORDER BY "Id";

SELECT "Id", "Name", "CreatedDate", "CreatorId",
       "LastModifiedDate", "LastModifierId",
       "IsDeleted", "DeletedDate", "DeleterId"
FROM "Tags"
ORDER BY "Id";

SELECT "Id", "TaskItemId", "TagId", "CreatedDate"
FROM "TaskTags"
ORDER BY "Id";

SELECT "TaskItemId", "AssignedUserId", "AssignedDate", "IsCompleted", "CompletedDate"
FROM "TaskAssignments"
ORDER BY "TaskItemId", "AssignedUserId";
```

Hedefli soft-delete kontrolü:

```sql
SELECT "Id", "Name", "IsDeleted", "DeletedDate", "DeleterId"
FROM "Tags"
WHERE "Id" = <deletedTagId>;
```

Hedefli fiziksel TaskTag silme kontrolü; sonuç **0 satır** olmalıdır:

```sql
SELECT "Id", "TaskItemId", "TagId"
FROM "TaskTags"
WHERE "Id" = <deletedTaskTagId>;
```

Her işlemde üç kanıtı birlikte kaydedin: HTTP durumu/gövdesi, takip eden GET sonucu ve PostgreSQL satır durumu. Yalnızca HTTP `200/204` görmek kalıcılık kanıtı değildir.
