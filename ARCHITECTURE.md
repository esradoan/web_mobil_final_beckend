# Smart Campus Architecture

## System Overview

The Smart Campus system follows a **Monolithic Layered Architecture**, designed for simplicity and ease of development while maintaining clear separation of concerns.

## 🏗️ Architecture Diagram

```mermaid
graph TD
    Client[Web & Mobile Clients] -->|HTTPS/JSON| API[SmartCampus.API]
    Client -->|WSS/SignalR| Hubs[SignalR Hubs]
    
    subgraph "Backend System"
        API --> Auth[Auth Controller]
        API --> Academic[Academic Controller]
        API --> IoT[IoT Controller]
        
        API --> Services[Business Services Layer]
        
        Services -->|Logic| Repos[Data Access (DbContext)]
        Services -->|Events| Hubs
        
        Repos -->|EF Core| DB[(MySQL Database)]
    end
```

## 🧩 Technology Choices

| Component | Technology | Rationale |
| -- | -- | -- |
| **Backend Framework** | .NET 8 Web API | High performance, strong typing, mature ecosystem. |
| **Database** | MySQL 8.0 | Cost-effective, reliable relational database. |
| **ORM** | Entity Framework Core | Accelerates development with Code-First approach. |
| **Real-time** | SignalR | Built-in support for WebSocket scaling. |
| **Auth** | ASP.NET Identity (JWT) | Secure, standard stateless authentication. |

## 📐 Design Patterns

1. **Dependency Injection (DI)**: Thoroughly used across all layers (`IServiceCollection`).
2. **Repository Pattern (via EF Core)**: `DbContext` acts as the Unit of Work and Repository.
3. **DTO Pattern**: Separation of Entity models from API contracts (`EnrollmentDto`, `GradeInputDto`).
4. **Service Layer Pattern**: All business logic resides in `SmartCampus.Business`, not Controllers.

## 🔄 Data Flow

1. **Request**: Client sends JWT-authenticated request to API.
2. **Validation**: API layer validates DTOs (Data Transfer Objects).
3. **Business Logic**: Service layer performs checks (e.g., Prerequisites, Time Conflicts).
4. **Data Access**: EF Core translates LINQ queries to SQL.
5. **Side Effects**: Service triggers Email/Notification/SignalR events.
6. **Response**: Data is mapped back to DTOs and returned to client.

## 📂 Project Structure

- `SmartCampus.API`: Entry point, Controllers, Hubs, Middlewares.
- `SmartCampus.Business`: Services, DTOs, Business Logic.
- `SmartCampus.DataAccess`: DbContext, Migrations.
- `SmartCampus.Entities`: POCO classes representing database tables.
- `SMARTCAMPUS.Tests`: xUnit tests.
