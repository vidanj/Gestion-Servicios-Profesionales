# Modelo de datos actual (ingeniería inversa)

> Fuente: `backend/SistemaServicios.API/Models/*.cs`, `Data/AppDbContext.cs` y
> `Migrations/AppDbContextModelSnapshot.cs` en `dev` @ 0159a9b (2026-09-11).
> Nombres de tabla = nombres de los `DbSet` (convención de EF Core). Motor: PostgreSQL.

## 1. Diagrama entidad-relación

```mermaid
erDiagram
    Users ||--o{ Services : "ofrece (ProfessionalId)"
    Categories ||--o{ Services : "clasifica"
    Users ||--o{ Requests : "solicita (ClientId)"
    Users ||--o{ Requests : "atiende (ProfessionalId)"
    Services ||--o{ Requests : "se solicita"
    Requests ||--o{ Ratings : "se califica"
    Users ||--o{ Ratings : "emite (ClientId)"
    Users ||--o{ Ratings : "recibe (ProfessionalId)"
    Services ||--o{ Quotes : "se cotiza"
    Users ||--o{ Quotes : "pide (ClientId)"
    Users ||--o{ Verifications : "acredita (ProfessionalId)"
    Users ||--o{ UserLogs : "genera"
    Users ||--o{ StoredFiles : "posee (OwnerUserId)"

    Users {
        uuid Id PK
        string Email UK "índice único"
        string PasswordHash "BCrypt"
        string FirstName "máx. 255"
        string LastName "máx. 255"
        int Role "UserRole: Admin 0, Client 1, Professional 2"
        string PhoneNumber "máx. 20, opcional"
        decimal AverageRating "18,2 - nunca se actualiza"
        bool Status "soft delete; índice parcial Status = true"
        string ProfileImageUrl "máx. 2048, opcional"
        timestamp CreatedAt "índice"
        timestamp UpdatedAt
    }
    Categories {
        int Id PK
        string Name "máx. 200"
        string Description "máx. 1000, opcional"
        string IconUrl "máx. 2048, opcional"
        bool IsActive
        timestamp CreatedAt
        timestamp UpdatedAt "opcional"
    }
    Services {
        int Id PK
        uuid ProfessionalId FK
        int CategoryId FK
        string Title "máx. 200"
        string Description "máx. 1000, opcional"
        decimal BasePrice "18,2"
        string ImageUrl "máx. 2048, opcional"
        bool IsActive
        timestamp CreatedAt
        timestamp UpdatedAt "opcional"
    }
    Requests {
        int Id PK
        uuid ClientId FK
        uuid ProfessionalId FK "índice compuesto con RequestDate"
        int ServiceId FK
        int Status "RequestStatus"
        decimal QuotedPrice "18,2 - copia BasePrice al crear"
        string Description "máx. 1000, opcional"
        timestamp RequestDate
        timestamp ScheduledDate "opcional"
        timestamp CompletionDate "opcional"
    }
    Quotes {
        int Id PK
        int ServiceId FK
        uuid ClientId FK
        decimal EstimatedPrice "18,2"
        int EstimatedDuration
        string Description "máx. 1000, opcional"
        int Status "QuoteStatus"
        timestamp ValidUntil "opcional"
        timestamp CreatedAt
    }
    Ratings {
        int Id PK
        int RequestId FK "sin índice único"
        uuid ClientId FK
        uuid ProfessionalId FK "redundante con Requests.ProfessionalId"
        int Score "rango 1-5"
        string Comment "máx. 500, opcional"
        timestamp CreatedAt
    }
    Verifications {
        int Id PK
        uuid ProfessionalId FK
        int DocumentType "DocumentType (10 valores)"
        string DocumentUrl "máx. 2048"
        int Status "VerificationStatus"
        string ExternalReference "máx. 2048"
        timestamp SubmittedAt
        timestamp VerifiedAt "opcional"
        timestamp ExpiresAt "opcional"
    }
    UserLogs {
        uuid Id PK
        uuid UserId FK
        int Action "LogAction (11 valores)"
        string Detail "máx. 500, opcional"
        int Status "LogStatus"
        string TraceId "máx. 32, opcional"
        timestamp CreatedAt
    }
    StoredFiles {
        uuid Id PK
        uuid OwnerUserId FK "índice"
        bytea Content
        string ContentType "máx. 100"
        bigint SizeBytes
        timestamp CreatedAt
    }
```

## 2. Comportamiento al borrar

| Relación (hijo → padre) | FK | `OnDelete` | Origen |
|---|---|---|---|
| Services → Users | `ProfessionalId` | **Cascade** | Convención de EF (FK requerida) |
| Services → Categories | `CategoryId` | **Cascade** | Convención |
| Requests → Users (cliente) | `ClientId` | Restrict | Explícito en `AppDbContext` |
| Requests → Users (profesional) | `ProfessionalId` | Restrict | Explícito |
| Requests → Services | `ServiceId` | **Cascade** | Convención |
| Ratings → Requests | `RequestId` | **Cascade** | Convención |
| Ratings → Users (cliente / profesional) | `ClientId`, `ProfessionalId` | Restrict | Explícito |
| Quotes → Users | `ClientId` | **Cascade** | Convención |
| Quotes → Services | `ServiceId` | **Cascade** | Convención |
| Verifications → Users | `ProfessionalId` | **Cascade** | Convención |
| UserLogs → Users | `UserId` | Restrict | Explícito |
| StoredFiles → Users | `OwnerUserId` | Restrict | Explícito |

## 3. Enumeraciones

| Enum | Valores |
|---|---|
| `UserRole` | Admin = 0, Client = 1, Professional = 2 |
| `RequestStatus` | Pending = 0, Accepted = 1, InProgress = 2, Completed = 3, Cancelled = 4 |
| `QuoteStatus` | Pending = 0, Accepted = 1, Rejected = 2, Expired = 3 |
| `VerificationStatus` | Pending = 0, Verified = 1, Rejected = 2 |
| `DocumentType` | TituloProfesional = 1 … LicenciaTecnica = 10 |
| `LogAction` | CambioContrasena = 0 … EliminacionServicio = 10 |
| `LogStatus` | Exitoso = 0, Alerta = 1, Error = 2 |

## 4. Índices

| Tabla | Índice | Migración |
|---|---|---|
| Users | `Email` único | `AddUserIndexes` (2026-08-09) |
| Users | `CreatedAt` | `AddUserIndexes` |
| Users | `Status` parcial (`"Status" = true`) | `AddUserIndexes` |
| Requests | `(ProfessionalId, RequestDate)` | `AddRequestCompositeIndex` (2026-08-09) |
| StoredFiles | `OwnerUserId` | `AddStoredFiles` (2026-08-13) |
| (todas las FK) | Índice por FK | Convención de EF |

## 5. Ciclo de vida implementado de una solicitud

Extraído de `ServiceRequestService.IsValidTransition`. Solo el profesional asignado o un Admin
cambian el estado; `CompletionDate` se fija al completar.

```mermaid
stateDiagram-v2
    [*] --> Pending : cliente crea la solicitud (QuotedPrice = BasePrice)
    Pending --> Accepted
    Pending --> Cancelled
    Accepted --> InProgress
    Accepted --> Cancelled
    InProgress --> Completed : fija CompletionDate
    InProgress --> Cancelled
    Completed --> [*]
    Cancelled --> [*]
```

`QuoteStatus` y `VerificationStatus` están declarados pero **ningún código los usa**: no existe
lógica de transición para cotizaciones ni verificaciones.

## 6. Historia del esquema

| Fecha | Migración | Cambio |
|---|---|---|
| 2026-02-18 | `InitialCreate` | Users, Categories, Services, Requests, Quotes, Ratings, Verifications |
| 2026-03-11 | `AddUserLogs` | Bitácora de acciones |
| 2026-03-14 | `SeedCategories` | Datos iniciales de categorías |
| 2026-08-09 | `AddUserIndexes` | Índices de Users |
| 2026-08-09 | `AddRequestCompositeIndex` | Índice compuesto de Requests |
| 2026-08-13 | `AddStoredFiles` | Avatares en base de datos |
| 2026-08-14 | `AddTraceIdToUserLog` | Enlace bitácora ↔ log de operación |

## 7. Observaciones del modelo

| # | Observación | Riesgo | Referencia |
|---|---|---|---|
| M1 | **Cadena de borrado en cascada** Categories → Services → Requests → Ratings. Borrar una categoría eliminaría servicios, solicitudes y calificaciones. Hoy ninguna API borra categorías, así que el riesgo está latente | Alto (pérdida de datos) | §2 |
| M2 | `Quotes` no se relaciona con `Requests`: una cotización nunca alimenta una solicitud; `QuotedPrice` copia el precio base | Funcional | `ServiceRequestService.CreateRequestAsync` |
| M3 | `Ratings.ProfessionalId` es redundante con `Requests.ProfessionalId` y se toma del DTO del cliente | Integridad | `RatingService.CreateRatingAsync` |
| M4 | No hay índice único `(RequestId, ClientId)` en Ratings: la unicidad solo se valida en la aplicación (condición de carrera posible) | Integridad | `IRatingRepository.ExistsRatingForRequestAsync` |
| M5 | `Users.AverageRating` está desnormalizado y nunca se actualiza (el promedio se calcula al vuelo en otro endpoint) | Dato engañoso | `RatingService.GetProfessionalAverageRatingAsync` |
| M6 | Borrado inconsistente: Users → Services, Quotes y Verifications en Cascade; Users → Requests y Ratings en Restrict. El soft delete lo mitiga mientras nadie borre físicamente | Medio | §2 |
| M7 | `Verifications.DocumentUrl` es texto libre aunque ya existe `StoredFiles` para binarios | Diseño | `Verification.cs` |
| M8 | Nada garantiza a nivel de datos que `Services.ProfessionalId` apunte a un usuario con rol Professional | Integridad | `Service.cs` |
| M9 | Identificadores mixtos: `uuid` en Users, UserLogs y StoredFiles; `int` en el resto | Bajo | — |
