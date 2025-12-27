# Smart Campus Backend System

Smart Campus is a comprehensive university management system designed to streamline academic operations, attendance tracking, and campus services. This repository contains the **Backend API** built with ASP.NET Core 8.

## 🚀 Features

### Core Modules
- **Authentication**: JWT-based auth with Role-Based Access Control (Student, Faculty, Admin)
- **Academic Management**: Courses, Sections, Enrollments, Grades, Transcripts
- **Attendance Tracking**: QR Code & GPS-based attendance with real-time validation
- **Campus Life**: Meal menu management, event scheduling, and digital wallet

### Advanced Features (Part 4)
- **Notification System**: Real-time alerts via SignalR + Database & Email fallback
- **Analytics & Reporting**: PDF/Excel exports for attendance and section reports
- **IoT Integration**: Real-time sensor monitoring (Temperature, Noise, Light) for classrooms

## 🛠️ Tech Stack

- **Framework**: .NET 8 (ASP.NET Core Web API)
- **Database**: MySQL 8.0 (Entity Framework Core 8)
- **Real-time**: SignalR (WebSockets)
- **Reporting**: QuestPDF, CSV Helper
- **Testing**: xUnit, Moq, EF Core InMemory
- **Documentation**: Swagger/OpenAPI

## 📐 Architecture

The system follows a **Monolithic Layered Architecture** with clear separation of concerns:

- **SmartCampus.API**: Entry point, Controllers, SignalR Hubs, Middlewares
- **SmartCampus.Business**: Services, DTOs, Business Logic
- **SmartCampus.DataAccess**: DbContext, Migrations, Repositories
- **SmartCampus.Entities**: POCO classes representing database tables

### Design Patterns
- **Dependency Injection (DI)**: Used across all layers
- **Repository Pattern**: `DbContext` acts as Unit of Work and Repository
- **DTO Pattern**: Separation of Entity models from API contracts
- **Service Layer Pattern**: All business logic in `SmartCampus.Business`

## 📦 Installation

### Prerequisites
- .NET 8 SDK
- MySQL Server (8.0+)
- Visual Studio 2022 or VS Code

### Quick Start

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd web_mobil_final_beckend
   ```

2. **Configure Database**
   Update `appsettings.Development.json` with your MySQL connection string:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=smart_campus_db;User=root;Password=your_password;Port=3306;"
   }
   ```

3. **Run the application**
   ```bash
   cd SmartCampus.API
   dotnet run
   ```
   The application automatically handles migrations on startup.

4. **Access Swagger**
   Open `http://localhost:5226/swagger`

## 🐳 Docker Deployment

### Local Development
```bash
docker-compose up --build -d
```

### Production (Railway)
The project includes:
- `Dockerfile` for containerized builds
- `railway.json` for Railway deployment configuration
- Automatic migration handling via `DbMigrationHelper`

## 📡 API Endpoints

Base URL: `/api/v1`

### Authentication
- `POST /auth/register` - Create student account
- `POST /auth/login` - Authenticate and get tokens
- `POST /auth/refresh-token` - Get new access token

### Academic
- `GET /courses` - List all courses
- `GET /sections` - List open sections
- `POST /enrollments` - Enroll in a section
- `GET /enrollments/my-courses` - List student's courses
- `GET /enrollments/my-grades` - View grades
- `POST /enrollments/grade` - (Faculty) Enter grades

### Attendance
- `POST /attendance/sessions` - (Faculty) Create QR session
- `POST /attendance/qr-code` - (Student) Check-in via QR
- `GET /attendance/sessions/{id}/records` - View attendance list

### Notifications (Part 4)
- `GET /notifications` - List notifications
- `PUT /notifications/{id}/read` - Mark as read
- **SignalR Hub**: `/hubs/notifications`

### Analytics (Part 4)
- `GET /analytics/campus` - Campus-wide stats (Admin)
- `GET /analytics/sections/{id}/export/pdf` - Download PDF report
- `GET /analytics/sections/{id}/export/excel` - Download Excel report

### IoT (Part 4)
- `GET /sensors` - List all sensors
- `GET /sensors/{id}/history` - Get historical data
- **SignalR Hub**: `/hubs/sensors`

> **Full API Documentation**: See Swagger UI at `/swagger`

## 🧪 Testing

```bash
cd SmartCampus.Tests
dotnet test
```

Test coverage includes:
- Notification Service & Controller
- IoT System Controller
- Data Privacy & Security

## 🗄️ Database

- **Database**: MySQL 8.0
- **ORM**: Entity Framework Core 8
- **Migrations**: Automatic on startup (via `DbMigrationHelper`)
- **Schema**: See `database_diagram.dbml` for visual representation

## 🔧 Development

### Project Structure
```
SmartCampus.API/          # Controllers, Hubs, Middleware
SmartCampus.Business/     # Services, DTOs, Business Logic
SmartCampus.DataAccess/   # DbContext, Migrations
SmartCampus.Entities/     # Entity Models
SmartCampus.Tests/        # Unit Tests
```

### Coding Conventions
- **Naming**: PascalCase for methods/classes, camelCase for variables
- **Async/Await**: Always use `async Task` for I/O operations
- **DI**: Constructor Injection for dependencies
- **Error Handling**: Throw specific exceptions in Service layer

### Adding New Features
1. Create feature branch: `feature/feature-name`
2. Implement in appropriate layer (Business → DataAccess → API)
3. Write tests
4. Update Swagger documentation
5. Commit and create Pull Request

## 📊 Deployment

### Environment Variables
- `ConnectionStrings__DefaultConnection` - MySQL connection string
- `JWT__Key` - JWT signing key
- `JWT__Issuer` - Token issuer
- `Email__SmtpHost` - SMTP server
- `ASPNETCORE_ENVIRONMENT` - Runtime environment

### Railway Deployment
1. Connect GitHub repository to Railway
2. Configure environment variables
3. Railway automatically builds using `Dockerfile`
4. Database migrations run automatically on startup

## 👥 Contributors

- **Backend Team**: Smart Campus Development Team
- **Frontend Team**: Smart Campus Frontend Team

## 📄 License

MIT License
